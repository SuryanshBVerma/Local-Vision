var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

// Configure HTTPS redirection
app.UseHttpsRedirection();

app.MapReverseProxy();

app.Run();
