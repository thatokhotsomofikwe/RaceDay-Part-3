using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;

namespace RaceDay.API.Services
{
    public class BlobStorageService
    {
        private readonly BlobContainerClient _containerClient;

        public BlobStorageService(IConfiguration configuration)
        {
            string connectionString =
                configuration["AzureBlobStorage:ConnectionString"]
                ?? throw new InvalidOperationException(
                    "Azure Blob Storage connection string is missing.");

            string containerName =
                configuration["AzureBlobStorage:ContainerName"]
                ?? "raceday-event-images";

            var blobServiceClient =
                new BlobServiceClient(connectionString);

            _containerClient =
                blobServiceClient.GetBlobContainerClient(containerName);
        }

        public async Task<string> UploadImageAsync(
            Stream imageStream,
            string fileName,
            string contentType)
        {
            await _containerClient.CreateIfNotExistsAsync(
                PublicAccessType.None);

            string uniqueFileName =
                $"{Guid.NewGuid()}{Path.GetExtension(fileName)}";

            BlobClient blobClient =
                _containerClient.GetBlobClient(uniqueFileName);

            await blobClient.UploadAsync(
                imageStream,
                new BlobHttpHeaders
                {
                    ContentType = contentType
                });

            return blobClient.Uri.ToString();
        }

        public string GetReadImageUrl(string imageUrl)
        {
            if (!Uri.TryCreate(
                imageUrl,
                UriKind.Absolute,
                out var uri))
            {
                throw new ArgumentException(
                    "The image URL is invalid.", nameof(imageUrl));
            }

            string blobName =
                Uri.UnescapeDataString(uri.Segments[^1].TrimEnd('/'));

            BlobClient blobClient =
                _containerClient.GetBlobClient(blobName);

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _containerClient.Name,
                BlobName = blobName,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.AddHours(1)
            };

            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            return blobClient.GenerateSasUri(sasBuilder).ToString();
        }
    }
}