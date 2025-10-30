using Microsoft.AspNetCore.Mvc;
using Minio;
using Minio.ApiEndpoints;
using Minio.DataModel;
using Minio.DataModel.Args;
using Minio.Exceptions;
using System.Text.Json;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/storage")]
    public class StorageController : ControllerBase
    {
        private readonly IMinioClient _minio;

        public StorageController(IMinioClient minio) => _minio = minio;

        // Create a bucket
        [HttpPost("bucket/{bucketName}")]
        public async Task<IActionResult> CreateBucket(string bucketName)
        {
            var exists = await _minio.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucketName));
            if (exists)
                return Conflict(new { message = $"Bucket '{bucketName}' already exists." });

            await _minio.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucketName));
            return Ok(new { message = $"Bucket '{bucketName}' created successfully." });
        }

        // Delete a bucket
        [HttpDelete("bucket/{bucketName}")]
        public async Task<IActionResult> DeleteBucket(string bucketName, [FromQuery] bool force = false)
        {
            var exists = await _minio.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucketName));
            if (!exists)
                return NotFound(new { message = $"Bucket '{bucketName}' does not exist." });

            try
            {
                if (force)
                {
                    // Delete all objects in the bucket before removing it
                    var objectsToDelete = new List<string>();

                    await foreach (var obj in _minio.ListObjectsEnumAsync(
                        new ListObjectsArgs()
                            .WithBucket(bucketName)
                            .WithRecursive(true)))
                    {
                        objectsToDelete.Add(obj.Key);
                    }

                    if (objectsToDelete.Count > 0)
                    {
                        await _minio.RemoveObjectsAsync(new RemoveObjectsArgs()
                            .WithBucket(bucketName)
                            .WithObjects(objectsToDelete));

                        // Give a brief delay to ensure all deletions are committed
                        await Task.Delay(200);
                    }

                    await _minio.RemoveBucketAsync(new RemoveBucketArgs().WithBucket(bucketName));
                    return Ok(new { message = $"Bucket '{bucketName}' and all contents deleted successfully." });
                }
                else
                {
                    // Try deleting normally first
                    await _minio.RemoveBucketAsync(new RemoveBucketArgs().WithBucket(bucketName));
                    return Ok(new { message = $"Bucket '{bucketName}' deleted successfully." });
                }
            }
            catch (MinioException ex) when (ex.Message.Contains("The bucket you tried to delete is not empty", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new
                {
                    message = $"Bucket '{bucketName}' is not empty. Retry with '?force=true' to delete all objects and the bucket."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An unexpected error occurred.", details = ex.Message });
            }
        }


        // Add object with description (metadata)
        [HttpPost("bucket/{bucketName}/upload")]
        public async Task<IActionResult> UploadObject(string bucketName, [FromForm] IFormFile file, [FromForm] string description)
        {
            var exists = await _minio.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucketName));
            if (!exists)
                return NotFound(new { message = $"Bucket '{bucketName}' does not exist." });

            // Validate file type (only images)
            var allowedContentTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp", "image/bmp" };
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp" };

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedContentTypes.Contains(file.ContentType.ToLower()) || !allowedExtensions.Contains(extension))
                return BadRequest(new { message = "Only image files (JPG, PNG, GIF, WEBP, BMP) are allowed." });

            var objectName = file.FileName;

            using var stream = file.OpenReadStream();
            var metadata = new Dictionary<string, string>
            {
                { "x-amz-meta-description", description },
                { "x-amz-meta-originalname", file.FileName }
            };

            var response = await _minio.PutObjectAsync(new PutObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithStreamData(stream)
                .WithObjectSize(stream.Length)
                .WithContentType(file.ContentType)
                .WithHeaders(metadata));

            return Ok(new
            {
                id = response.Etag?.Trim('"'),
                objectName,
                description
            });
        }


        // List all buckets
        [HttpGet("buckets")]
        public async Task<IActionResult> ListBuckets()
        {
            var result = await _minio.ListBucketsAsync();
            var buckets = result.Buckets.Select(b => new
            {
                name = b.Name,
                created = b.CreationDate
            });

            return Ok(buckets);
        }

        // List all objects in a bucket with metadata
        [HttpGet("bucket/{bucketName}/objects")]
        public async Task<IActionResult> ListObjects(string bucketName)
        {
            var exists = await _minio.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucketName));
            if (!exists)
                return NotFound(new { message = $"Bucket '{bucketName}' does not exist." });

            var objects = new List<object>();

            await foreach (var item in _minio.ListObjectsEnumAsync(
                new ListObjectsArgs()
                    .WithBucket(bucketName)
                    .WithRecursive(true)))
            {
                objects.Add(new
                {
                    item.Key,
                    item.Size,
                    item.LastModified,
                    item.ETag
                });
            }

            return Ok(objects);
        }


        // Fetch an object by ID
        [HttpGet("bucket/{bucketName}/object/{etag}")]
        public async Task<IActionResult> GetObjectById(string bucketName, string etag)
        {
            var exists = await _minio.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucketName));
            if (!exists)
                return NotFound(new { message = $"Bucket '{bucketName}' does not exist." });

            string? objectName = null;

            await foreach (var item in _minio.ListObjectsEnumAsync(
                new ListObjectsArgs()
                    .WithBucket(bucketName)
                    .WithRecursive(true)))
            {
                if (string.Equals(item.ETag?.Trim('"'), etag.Trim('"'), StringComparison.OrdinalIgnoreCase))
                {
                    objectName = item.Key;
                    break;
                }
            }

            if (objectName == null)
                return NotFound(new { message = $"Object with ETag '{etag}' not found in bucket '{bucketName}'." });

            var stream = new MemoryStream();

            await _minio.GetObjectAsync(new GetObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithCallbackStream(s => s.CopyTo(stream)));

            stream.Position = 0;

            return File(stream, "application/octet-stream", objectName);
        }


        
        [HttpDelete("bucket/{bucketName}/object/{etag}")]
        public async Task<IActionResult> DeleteObjectByETag(string bucketName, string etag)
        {

            var exists = await _minio.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucketName));
            if (!exists)
                return NotFound(new { message = $"Bucket '{bucketName}' does not exist." });

            string? objectName = null;

            await foreach (var obj in _minio.ListObjectsEnumAsync(
                new ListObjectsArgs().WithBucket(bucketName).WithRecursive(true)))
            {
                if (string.Equals(obj.ETag, etag, StringComparison.OrdinalIgnoreCase))
                {
                    objectName = obj.Key;
                    break;
                }
            }

            if (objectName == null)
                return NotFound(new { message = $"No object found in bucket '{bucketName}' with ETag '{etag}'." });

            try
            {
                await _minio.RemoveObjectAsync(new RemoveObjectArgs()
                    .WithBucket(bucketName)
                    .WithObject(objectName));

                return Ok(new
                {
                    message = $"Object '{objectName}' deleted successfully from bucket '{bucketName}'.",
                    etag
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to delete object.", error = ex.Message });
            }
        }

    }
}
