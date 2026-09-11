using System.Globalization;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using CoffeeNChill.Models;

namespace CoffeeNChill.Services
{
    public class StaffDocumentService
    {
        private const string ShareName = "staff-docs";
        private const string ConnectionSettingName = "StaffDocsStorage";
        private const string ConnectionPlaceholder = "PASTE_AZURE_STORAGE_CONNECTION_STRING_HERE";

        private ShareClient GetShareClient()
        {
            string? connectionString = Environment.GetEnvironmentVariable(ConnectionSettingName);

            if (string.IsNullOrWhiteSpace(connectionString) ||
                string.Equals(connectionString, ConnectionPlaceholder, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Set {ConnectionSettingName} in local.settings.json to an Azure Storage connection string.");
            }

            return new ShareClient(connectionString, ShareName);
        }

        public async Task<StaffDocumentInfo> UploadAsync(
            string fileName,
            long fileSize,
            string contentType,
            Stream source,
            CancellationToken cancellationToken = default)
        {
            string safeFileName = GetSafeFileName(fileName);
            ShareClient shareClient = GetShareClient();
            await shareClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

            ShareDirectoryClient rootDirectory = shareClient.GetRootDirectoryClient();
            ShareFileClient fileClient = rootDirectory.GetFileClient(safeFileName);

            var headers = new ShareFileHttpHeaders
            {
                ContentType = contentType
            };

            var metadata = new Dictionary<string, string>
            {
                ["contenttype"] = contentType,
                ["uploadedutc"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                ["size"] = fileSize.ToString(CultureInfo.InvariantCulture)
            };

            await fileClient.CreateAsync(
                fileSize,
                new ShareFileCreateOptions
                {
                    HttpHeaders = headers,
                    Metadata = metadata
                },
                cancellationToken: cancellationToken);

            await using (Stream destination = await fileClient.OpenWriteAsync(
                overwrite: false,
                position: 0,
                cancellationToken: cancellationToken))
            {
                await source.CopyToAsync(destination, cancellationToken);
            }

            ShareFileProperties properties =
                (await fileClient.GetPropertiesAsync(cancellationToken: cancellationToken)).Value;

            return MapDocument(safeFileName, properties);
        }

        public async Task<List<StaffDocumentInfo>> ListAsync(
            CancellationToken cancellationToken = default)
        {
            ShareClient shareClient = GetShareClient();
            await shareClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

            ShareDirectoryClient rootDirectory = shareClient.GetRootDirectoryClient();
            var documents = new List<StaffDocumentInfo>();

            await foreach (ShareFileItem item in rootDirectory
                .GetFilesAndDirectoriesAsync(cancellationToken: cancellationToken))
            {
                if (item.IsDirectory)
                {
                    continue;
                }

                ShareFileClient fileClient = rootDirectory.GetFileClient(item.Name);
                ShareFileProperties properties =
                    (await fileClient.GetPropertiesAsync(cancellationToken: cancellationToken)).Value;

                documents.Add(MapDocument(item.Name, properties));
            }

            return documents
                .OrderBy(document => document.FileName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public async Task<(Stream Content, string ContentType, string FileName)?> DownloadAsync(
            string fileName,
            CancellationToken cancellationToken = default)
        {
            string safeFileName = GetSafeFileName(fileName);
            ShareClient shareClient = GetShareClient();

            if (!(await shareClient.ExistsAsync(cancellationToken)).Value)
            {
                return null;
            }

            ShareFileClient fileClient = shareClient
                .GetRootDirectoryClient()
                .GetFileClient(safeFileName);

            if (!(await fileClient.ExistsAsync(cancellationToken)).Value)
            {
                return null;
            }

            ShareFileDownloadInfo download =
                (await fileClient.DownloadAsync(cancellationToken: cancellationToken)).Value;

            string contentType = string.IsNullOrWhiteSpace(download.ContentType)
                ? "application/octet-stream"
                : download.ContentType;

            return (download.Content, contentType, safeFileName);
        }

        private static StaffDocumentInfo MapDocument(
            string fileName,
            ShareFileProperties properties)
        {
            return new StaffDocumentInfo
            {
                FileName = fileName,
                Size = properties.ContentLength,
                LastModified = properties.LastModified,
                ContentType = string.IsNullOrWhiteSpace(properties.ContentType)
                    ? "application/octet-stream"
                    : properties.ContentType,
                Metadata = new Dictionary<string, string>(
                    properties.Metadata,
                    StringComparer.OrdinalIgnoreCase)
            };
        }

        private static string GetSafeFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("A file name is required.", nameof(fileName));
            }

            string safeFileName = Path.GetFileName(fileName);

            if (!string.Equals(safeFileName, fileName, StringComparison.Ordinal) ||
                fileName.Contains('/') ||
                fileName.Contains('\\'))
            {
                throw new ArgumentException("The file name is invalid.", nameof(fileName));
            }

            return safeFileName;
        }
    }
}
