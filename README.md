# Local-Vision

Local-Vision is a multi-service application for storing, searching, and managing images with automatically generated captions. It uses a combination of .NET and Python services to provide a complete solution for local image management.

## Project Structure

The project is composed of the following services:

- **`Gateway`**: An ASP.NET Core reverse proxy that acts as the single entry point for the application. It routes requests to the appropriate backend services. All external traffic should go through the Gateway.

- **`Backend`**: An ASP.NET Core service that provides the main API for interacting with the application. It handles image uploads to a Minio object storage, manages image metadata, and orchestrates the image captioning and search workflows. When an image is uploaded, the Backend service sends it to the `image-caption-service` to generate a caption, and then stores the caption and its vector embedding in the `caption-vector-store`.

- **`image-caption-service`**: A Python FastAPI service that uses a pre-trained BLIP (Bootstrapping Language-Image Pre-training) model to generate descriptive captions for images. It exposes a simple endpoint that accepts an image and returns the generated caption.

- **`caption-vector-store`**: A Python FastAPI service responsible for storing and searching image captions. It uses a sentence transformer model to convert captions into vector embeddings, which are then stored in a FAISS (Facebook AI Similarity Search) index for efficient similarity searches. This enables finding images based on the semantic meaning of a search query rather than just keywords.

- **`Loacl-Vision-AppHost`**: A .NET Aspire application host project responsible for orchestrating and managing the various services in the Local-Vision application. It simplifies the development and deployment process by defining the dependencies and configurations for all the services in one place.

## Features

- **Image Upload**: Upload images to a Minio object storage.
- **Automatic Captioning**: Automatically generate captions for uploaded images using a BLIP model.
- **Vector-Based Search**: Search for images using natural language queries. The search is performed on the image captions using a vector similarity search.
- **Bucket Management**: Create and delete buckets to organize your images.
- **Object Management**: List and delete images from the object storage.

## Getting Started

To run the application, you will need to have Docker and .NET 9 installed.

1. **Clone the repository:**
   ```bash
   git clone https://github.com/your-username/Local-Vision.git
   ```
2. **Run the application:**
   - Using Docker Compose:
     ```bash
     docker-compose up -d
     ```
   - Using .NET Aspire:
     - Navigate to the `Loacl-Vision-AppHost` directory.
     - Run the application using the .NET CLI:
       ```bash
       dotnet run
       ```

## API Endpoints

The main API is exposed through the `Gateway` service. The following are some of the available endpoints:

- `POST /images/bucket/{bucketName}`: Create a new bucket.
- `DELETE /images/bucket/{bucketName}`: Delete a bucket.
- `POST /images/bucket/{bucketName}/upload`: Upload an image to a bucket.
- `GET /images/buckets`: List all buckets.
- `GET /images/bucket/{bucketName}/objects`: List all objects in a bucket.
- `GET /images/bucket/{bucketName}/object/{etag}`: Get an object by its ETag.
- `DELETE /images/bucket/{bucketName}/object/{etag}`: Delete an object by its ETag.
- `GET /images/search?query={query}`: Search for images using a natural language query.

