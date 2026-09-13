using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using CoffeeNChill.Models;

namespace CoffeeNChill.Services
{
    public class StaffDocumentService
    {
        private const string ShareName = "staff-docs";
        private const long MaxUploadSize = 10 * 1024 * 1024;

        private ShareClient GetShareClient()
        {
            var connectionString = Environment.GetEnvironmentVariable("StaffDocsStorage");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("StaffDocsStorage is not configured.");
            }

            return new ShareClient(connectionString, ShareName);
        }

        private async Task<ShareDirectoryClient> GetRootDirectoryAsync()
        {
            var shareClient = GetShareClient();
            await shareClient.CreateIfNotExistsAsync();
            return shareClient.GetRootDirectoryClient();
        }

        public async Task<StaffDocumentInfo> UploadAsync(string fileName, Stream source, string contentType)
        {
            var safeFileName = Path.GetFileName(fileName);

            if (string.IsNullOrWhiteSpace(safeFileName) ||
                !string.Equals(safeFileName, fileName, StringComparison.Ordinal))
            {
                throw new InvalidDataException("Invalid file name.");
            }

            var root = await GetRootDirectoryAsync();
            var fileClient = root.GetFileClient(safeFileName);
            long bytesWritten = 0;

            try
            {
                var options = new ShareFileOpenWriteOptions
                {
                    MaxSize = MaxUploadSize
                };

                await using (var destination = await fileClient.OpenWriteAsync(true, 0, options))
                {
                    var buffer = new byte[81920];
                    int bytesRead;

                    while ((bytesRead = await source.ReadAsync(buffer.AsMemory(0, buffer.Length))) > 0)
                    {
                        bytesWritten += bytesRead;

                        if (bytesWritten > MaxUploadSize)
                        {
                            throw new InvalidDataException("File exceeds the 10 MB upload limit.");
                        }

                        await destination.WriteAsync(buffer.AsMemory(0, bytesRead));
                    }
                }

                if (bytesWritten == 0)
                {
                    throw new InvalidDataException("The uploaded file is empty.");
                }

                var metadata = new Dictionary<string, string>
                {
                    ["contenttype"] = contentType,
                    ["uploadedutc"] = DateTimeOffset.UtcNow.ToString("O"),
                    ["size"] = bytesWritten.ToString()
                };

                await fileClient.SetHttpHeadersAsync(new ShareFileSetHttpHeadersOptions
                {
                    NewSize = bytesWritten,
                    HttpHeaders = new ShareFileHttpHeaders
                    {
                        ContentType = contentType
                    }
                });

                await fileClient.SetMetadataAsync(metadata);

                var properties = (await fileClient.GetPropertiesAsync()).Value;

                return new StaffDocumentInfo
                {
                    FileName = safeFileName,
                    Size = properties.ContentLength,
                    LastModified = properties.LastModified,
                    ContentType = properties.ContentType ?? "application/octet-stream",
                    Metadata = new Dictionary<string, string>(properties.Metadata)
                };
            }
            catch
            {
                await fileClient.DeleteIfExistsAsync();
                throw;
            }
        }

        public async Task<List<StaffDocumentInfo>> ListAsync()
        {
            var root = await GetRootDirectoryAsync();
            var documents = new List<StaffDocumentInfo>();

            await foreach (var item in root.GetFilesAndDirectoriesAsync())
            {
                if (item.IsDirectory)
                {
                    continue;
                }

                var fileClient = root.GetFileClient(item.Name);
                var properties = (await fileClient.GetPropertiesAsync()).Value;

                documents.Add(new StaffDocumentInfo
                {
                    FileName = item.Name,
                    Size = properties.ContentLength,
                    LastModified = properties.LastModified,
                    ContentType = properties.ContentType ?? "application/octet-stream",
                    Metadata = new Dictionary<string, string>(properties.Metadata)
                });
            }

            return documents;
        }

        public async Task<(Stream Content, string ContentType)?> DownloadAsync(string fileName)
        {
            var safeFileName = Path.GetFileName(fileName);

            if (string.IsNullOrWhiteSpace(safeFileName) ||
                !string.Equals(safeFileName, fileName, StringComparison.Ordinal))
            {
                return null;
            }

            var root = await GetRootDirectoryAsync();
            var fileClient = root.GetFileClient(safeFileName);

            if (!(await fileClient.ExistsAsync()).Value)
            {
                return null;
            }

            var properties = (await fileClient.GetPropertiesAsync()).Value;
            var stream = await fileClient.OpenReadAsync();

            return (stream, properties.ContentType ?? "application/octet-stream");
        }
    }
}
