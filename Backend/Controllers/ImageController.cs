using Microsoft.AspNetCore.Mvc;
using Minio;
using Minio.DataModel.Args;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/")]
    public class ImageController : ControllerBase
    {
        private readonly IMinioClient _minio;
        private const string BucketName = "images";

        public ImageController(IMinioClient minio) => _minio = minio;

        [HttpPost("upload")]
        public async Task<IActionResult> Upload([FromForm] IFormFile file)
        {
            using var stream = file.OpenReadStream();

            // Ensure the bucket exists before uploading
            var exists = await _minio.BucketExistsAsync(new BucketExistsArgs().WithBucket(BucketName));
            if (!exists)
            {
                await _minio.MakeBucketAsync(new MakeBucketArgs().WithBucket(BucketName));
            }

            await _minio.PutObjectAsync(new PutObjectArgs()
                .WithBucket(BucketName)
                .WithObject(file.FileName)
                .WithStreamData(stream)
                .WithObjectSize(stream.Length)
                .WithContentType(file.ContentType));

            return Ok(new { file.FileName });
        }

        [HttpGet("{name}")]
        public async Task<IActionResult> Get(string name)
        {
            var stream = new MemoryStream();
            await _minio.GetObjectAsync(new GetObjectArgs()
                .WithBucket(BucketName)
                .WithObject(name)
                .WithCallbackStream(x => x.CopyTo(stream)));

            stream.Position = 0;
            return File(stream, "application/octet-stream", name);
        }
    }

}
