using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Azure;
using Azure.Storage.Blobs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 104857600; // 100 MB
});

builder.Services.AddAzureClients(clientBuilder =>
{
    clientBuilder.AddBlobServiceClient(builder.Configuration["AZURE_BLOB_CONNECTION_STRING"]);
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseCors();

app.MapPost("/upload", async (HttpRequest request, BlobServiceClient blobServiceClient) =>
{
    var form = await request.ReadFormAsync();
    var file = form.Files["file"];

    if (file == null || file.Length == 0)
        return Results.BadRequest("Brak pliku.");

    var containerName = "uploads";
    var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
    //await containerClient.CreateIfNotExistsAsync();

    var blobClient = containerClient.GetBlobClient(file.FileName);

    using var stream = file.OpenReadStream();
    await blobClient.UploadAsync(stream, overwrite: true);

    return Results.Ok("Plik przesłany.");
});

app.Run();
