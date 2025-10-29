# save_model.py
from transformers import BlipProcessor, BlipForConditionalGeneration

MODEL_NAME = "Salesforce/blip-image-captioning-base"
SAVE_PATH = "./blip_model"

def save_model():
    print(f"⏬ Downloading and saving BLIP model ({MODEL_NAME}) to {SAVE_PATH} ...")
    processor = BlipProcessor.from_pretrained(MODEL_NAME)
    model = BlipForConditionalGeneration.from_pretrained(MODEL_NAME)

    processor.save_pretrained(SAVE_PATH)
    model.save_pretrained(SAVE_PATH)
    print("✅ Model and processor saved successfully!")

if __name__ == "__main__":
    save_model()
