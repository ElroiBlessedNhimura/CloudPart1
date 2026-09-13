using System.Net;
using CoffeeNChill.Services;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Net.Http.Headers;

namespace CoffeeNChill.Functions
{
    public class UploadStaffDocument
    {
        private readonly StaffDocumentService _staffDocumentService;

        private static readonly Dictionary<string, string[]> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = ["application/pdf"],
            [".txt"] = ["text/plain"],
            [".doc"] = ["application/msword"],
            [".docx"] = ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"]
        };

        public UploadStaffDocument(StaffDocumentService staffDocumentService)
        {
            _staffDocumentService = staffDocumentService;
        }

        [Function("UploadStaffDocument")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(
                AuthorizationLevel.Anonymous,
                "post",
                Route = "documents/upload")] HttpRequestData req)
        {
            if (!req.Headers.TryGetValues("Content-Type", out var contentTypeValues))
            {
                return await BadRequest(req, "Content-Type must be multipart/form-data.");
            }

            var requestContentType = contentTypeValues.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(requestContentType))
            {
                return await BadRequest(req, "Content-Type must be multipart/form-data.");
            }

            MediaTypeHeaderValue mediaType;

            try
            {
                mediaType = MediaTypeHeaderValue.Parse(requestContentType);
            }
            catch
            {
                return await BadRequest(req, "Invalid multipart request.");
            }

            if (!string.Equals(mediaType.MediaType.Value, "multipart/form-data", StringComparison.OrdinalIgnoreCase))
            {
                return await BadRequest(req, "Content-Type must be multipart/form-data.");
            }

            var boundary = HeaderUtilities.RemoveQuotes(mediaType.Boundary).Value;

            if (string.IsNullOrWhiteSpace(boundary))
            {
                return await BadRequest(req, "Multipart boundary is missing.");
            }

            var reader = new MultipartReader(boundary, req.Body);
            MultipartSection? section;

            while ((section = await reader.ReadNextSectionAsync()) != null)
            {
                var fileSection = section.AsFileSection();

                if (fileSection == null || !string.Equals(fileSection.Name, "file", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var fileName = Path.GetFileName(fileSection.FileName);
                var extension = Path.GetExtension(fileName);
                var contentType = section.ContentType;

                if (string.IsNullOrWhiteSpace(fileName) ||
                    string.IsNullOrWhiteSpace(contentType) ||
                    !AllowedTypes.TryGetValue(extension, out var allowedMimeTypes) ||
                    !allowedMimeTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase))
                {
                    return await BadRequest(req, "Unsupported file type.");
                }

                try
                {
                    var document = await _staffDocumentService.UploadAsync(
                        fileName,
                        fileSection.FileStream,
                        contentType);

                    var response = req.CreateResponse(HttpStatusCode.Created);
                    await response.WriteAsJsonAsync(document);
                    return response;
                }
                catch (InvalidDataException ex)
                {
                    return await BadRequest(req, ex.Message);
                }
            }

            return await BadRequest(req, "A file field named 'file' is required.");
        }

        private static async Task<HttpResponseData> BadRequest(HttpRequestData req, string message)
        {
            var response = req.CreateResponse(HttpStatusCode.BadRequest);
            await response.WriteAsJsonAsync(new { message });
            return response;
        }
    }
}
