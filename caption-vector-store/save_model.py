from sentence_transformers import SentenceTransformer
import os

MODEL_NAME = "all-MiniLM-L6-v2"
MODEL_DIR = os.path.join("models", MODEL_NAME)

def main():
    os.makedirs(MODEL_DIR, exist_ok=True)
    print(f"Downloading model '{MODEL_NAME}'...")
    model = SentenceTransformer(MODEL_NAME)
    model.save(MODEL_DIR)
    print(f"✅ Model saved locally at: {MODEL_DIR}")

if __name__ == "__main__":
    main()
