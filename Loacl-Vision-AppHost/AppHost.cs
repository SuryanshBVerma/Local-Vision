using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var minio = builder.AddContainer("minio", "quay.io/minio/minio")
    .WithArgs("server", "/data", "--console-address", ":9090")
    .WithEnvironment("MINIO_ROOT_USER", "admin")
    .WithEnvironment("MINIO_ROOT_PASSWORD", "password123")
    //.WithVolume("./minio/data", "/data")
    .WithEndpoint(name: "api", port: 9000, targetPort: 9000)      
    .WithEndpoint(name: "console", port: 9090, targetPort: 9090); 


var backend = builder.AddProject<Projects.Backend>("backend-service");

var gateway = builder.AddProject<Projects.Gateway>("gateway")
    .WithReference(backend);

builder.Build().Run();
