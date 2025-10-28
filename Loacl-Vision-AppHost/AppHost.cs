using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var minio = builder.AddContainer("minio", "quay.io/minio/minio")
    .WithArgs("server", "/data", "--console-address", ":9090")
    .WithEnvironment("MINIO_ROOT_USER", "admin")
    .WithEnvironment("MINIO_ROOT_PASSWORD", "password123")
    //.WithVolume("./minio/data", "/data")
    .WithEndpoint(9090, targetPort: 9090);


var imageService = builder.AddProject<Projects.Backend>("imageservice");

var gateway = builder.AddProject<Projects.Gateway>("gateway")
    .WithReference(imageService);

builder.Build().Run();
