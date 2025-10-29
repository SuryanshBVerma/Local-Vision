# main.py
from fastapi import FastAPI, UploadFile, File, HTTPException
from transformers import BlipProcessor, BlipForConditionalGeneration
from PIL import Image
from contextlib import asynccontextmanager
import torch
import os

MODEL_PATH = "./blip_model"

@asynccontextmanager
async def lifespan(app: FastAPI):
    print("⏳ Loading BLIP model...")
    if not os.path.exists(MODEL_PATH):
        raise RuntimeError(
            f"❌ Model not found at {MODEL_PATH}. Run 'uv run python save_model.py' first."
        )

    app.state.processor = BlipProcessor.from_pretrained(MODEL_PATH)
    app.state.model = BlipForConditionalGeneration.from_pretrained(MODEL_PATH)
    app.state.model.eval()
    print("✅ BLIP model loaded successfully.")
    yield
    print("🧹 Shutting down — releasing model resources.")
    del app.state.processor
    del app.state.model
    
app = FastAPI(
    title="BLIP Image Captioning Service",
    version="1.2",
    lifespan=lifespan,
)


@app.post("/caption")
async def caption_image(image: UploadFile = File(...)):
    try:
        img = Image.open(image.file).convert("RGB")
        inputs = app.state.processor(img, return_tensors="pt")

        with torch.no_grad():
            out = app.state.model.generate(**inputs, max_new_tokens=100)

        caption = app.state.processor.decode(out[0], skip_special_tokens=True)
        return {"caption": caption}

    except Exception as e:
        raise HTTPException(status_code=400, detail=str(e))


@app.get("/health")
def health():
    return {"status": "ok"}

