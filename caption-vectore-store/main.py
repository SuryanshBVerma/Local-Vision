# app.py
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
from sentence_transformers import SentenceTransformer
import faiss
import numpy as np
import sqlite3
import os
from pathlib import Path

# -------------------------
# Setup
# -------------------------
EMBED_DIM = 384
MODEL_NAME = "all-MiniLM-L6-v2"
DB_PATH = "./embeddings/captions.db"
INDEX_PATH = "./embeddings/captions.index"
LOCAL_MODEL_DIR = Path("models") / MODEL_NAME

os.makedirs("embeddings", exist_ok=True)

app = FastAPI(title="Caption Embedding Store")

# Load embedding model
if LOCAL_MODEL_DIR.exists():
    print(f"🔹 Loading local model from {LOCAL_MODEL_DIR}")
    model = SentenceTransformer(str(LOCAL_MODEL_DIR))
else:
    print(f"⚠️ Local model not found, downloading {MODEL_NAME}...")
    model = SentenceTransformer(MODEL_NAME)
    os.makedirs("models", exist_ok=True)
    model.save(str(LOCAL_MODEL_DIR))

# Load or initialize FAISS index
if os.path.exists(INDEX_PATH):
    index = faiss.read_index(INDEX_PATH)
else:
    index = faiss.IndexFlatIP(EMBED_DIM)
    index = faiss.IndexIDMap(index)

# Connect to SQLite (metadata)
conn = sqlite3.connect(DB_PATH, check_same_thread=False)
cur = conn.cursor()
cur.execute("""
CREATE TABLE IF NOT EXISTS captions (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    etag TEXT UNIQUE,
    caption TEXT
)
""")
conn.commit()

# -------------------------
# Request Models
# -------------------------
class AddCaptionRequest(BaseModel):
    etag: str
    caption: str

class SearchRequest(BaseModel):
    query: str
    limit: int = 5

# -------------------------
# Helper Functions
# -------------------------
def normalize(vecs: np.ndarray):
    faiss.normalize_L2(vecs)
    return vecs

def save_state():
    faiss.write_index(index, INDEX_PATH)
    conn.commit()

# -------------------------
# Routes
# -------------------------
@app.post("/add_caption")
def add_caption(req: AddCaptionRequest):
    # Check if caption already exists
    existing = cur.execute("SELECT id FROM captions WHERE etag = ?", (req.etag,)).fetchone()
    if existing:
        raise HTTPException(status_code=400, detail="Caption already exists for this ETAG")

    # Embed caption
    emb = model.encode([req.caption], convert_to_numpy=True)
    emb = normalize(emb.astype('float32'))

    # Insert into SQLite
    cur.execute("INSERT INTO captions (etag, caption) VALUES (?, ?)", (req.etag, req.caption))
    conn.commit()
    new_id = cur.lastrowid

    # Add to FAISS
    index.add_with_ids(emb, np.array([new_id], dtype='int64'))

    # Save
    save_state()

    return {"status": "success", "etag": req.etag}

@app.post("/search_captions")
def search_captions(req: SearchRequest):
    # Encode query
    emb = model.encode([req.query], convert_to_numpy=True)
    emb = normalize(emb.astype('float32'))

    # Search
    if index.ntotal == 0:
        raise HTTPException(status_code=404, detail="No captions indexed yet")

    distances, ids = index.search(emb, req.limit)

    results = []
    for dist, idx in zip(distances[0], ids[0]):
        if idx == -1:
            continue
        row = cur.execute("SELECT etag, caption FROM captions WHERE id = ?", (int(idx),)).fetchone()
        if row:
            results.append({
                "etag": row[0],
                "caption": row[1],
                "score": float(dist)
            })

    return {"results": results}
