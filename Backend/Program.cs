using Minio;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(sp =>
{
    return new Minio.MinioClient()
        .WithEndpoint("localhost:9000")
        .WithCredentials("admin", "password123")
        .WithSSL(false)
        .Build();
});

builder.Services.AddControllers();

builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
