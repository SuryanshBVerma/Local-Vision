# main.py
from fastapi import FastAPI, HTTPException
from transformers import BlipProcessor, BlipForConditionalGeneration
from PIL import Image
from contextlib import asynccontextmanager
import torch
import os
import requests
from pydantic import BaseModel, HttpUrl
from io import BytesIO

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

class ImageUrlRequest(BaseModel):
    image_url: HttpUrl


@app.post("/caption")
async def caption_image(request: ImageUrlRequest):
    try:
        # Download the image from the URL
        response = requests.get(request.image_url, timeout=10)
        response.raise_for_status()

        # Load it into Pillow
        img = Image.open(BytesIO(response.content)).convert("RGB")

        # Generate caption
        caption = generate_caption(app, img)
        return {"caption": caption, "source_url": request.image_url}

    except Exception as e:
        raise HTTPException(status_code=400, detail=f"Failed to process image: {str(e)}")

def generate_caption(app: FastAPI, img: Image.Image) -> str:
    prompt = "a detailed description of the image:"
    inputs = app.state.processor(img, return_tensors="pt")

    with torch.no_grad():
        out = app.state.model.generate(
            **inputs,
            max_new_tokens=50,
            num_beams=5,
            repetition_penalty=1.2,
        )

    return app.state.processor.decode(out[0], skip_special_tokens=True)


@app.get("/health")
def health():
    return {"status": "ok"}

