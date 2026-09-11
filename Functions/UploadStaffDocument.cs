using Azure;
using CoffeeNChill.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace CoffeeNChill.Functions
{
    public class UploadStaffDocument
    {
        private static readonly Dictionary<string, HashSet<string>> AllowedFileTypes =
            new(StringComparer.OrdinalIgnoreCase)
            {
                [".pdf"] = new(StringComparer.OrdinalIgnoreCase) { "application/pdf" },
                [".txt"] = new(StringComparer.OrdinalIgnoreCase) { "text/plain" },
                [".doc"] = new(StringComparer.OrdinalIgnoreCase) { "application/msword" },
                [".docx"] = new(StringComparer.OrdinalIgnoreCase)
                {
                    "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                }
            };

        private readonly StaffDocumentService _staffDocumentService;

        public UploadStaffDocument(StaffDocumentService staffDocumentService)
        {
            _staffDocumentService = staffDocumentService;
        }

        [Function("UploadStaffDocument")]
        public async Task<IActionResult> Run(
            [HttpTrigger(
                AuthorizationLevel.Anonymous,
                "post",
                Route = "documents/upload")] HttpRequest req)
        {
            if (!req.HasFormContentType)
            {
                return new BadRequestObjectResult(new
                {
                    error = "The request must use multipart/form-data."
                });
            }

            IFormCollection form;

            try
            {
                form = await req.ReadFormAsync(req.HttpContext.RequestAborted);
            }
            catch (InvalidDataException)
            {
                return new BadRequestObjectResult(new
                {
                    error = "The multipart/form-data request is invalid."
                });
            }

            IFormFile? file = form.Files.GetFile("file") ?? form.Files.FirstOrDefault();

            if (file == null)
            {
                return new BadRequestObjectResult(new
                {
                    error = "Add a file using the form-data key 'file'."
                });
            }

            if (file.Length <= 0)
            {
                return new BadRequestObjectResult(new
                {
                    error = "The uploaded file is empty."
                });
            }

            string? validationError = ValidateFile(file);

            if (validationError != null)
            {
                return new BadRequestObjectResult(new
                {
                    error = validationError
                });
            }

            try
            {
                await using Stream source = file.OpenReadStream();

                var document = await _staffDocumentService.UploadAsync(
                    file.FileName,
                    file.Length,
                    file.ContentType,
                    source,
                    req.HttpContext.RequestAborted);

                return new ObjectResult(document)
                {
                    StatusCode = StatusCodes.Status201Created
                };
            }
            catch (ArgumentException ex)
            {
                return new BadRequestObjectResult(new
                {
                    error = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                return new ObjectResult(new
                {
                    error = ex.Message
                })
                {
                    StatusCode = StatusCodes.Status503ServiceUnavailable
                };
            }
            catch (RequestFailedException)
            {
                return new ObjectResult(new
                {
                    error = "The file could not be uploaded to Azure Files."
                })
                {
                    StatusCode = StatusCodes.Status502BadGateway
                };
            }
        }

        private static string? ValidateFile(IFormFile file)
        {
            string extension = Path.GetExtension(file.FileName);

            if (!AllowedFileTypes.TryGetValue(extension, out HashSet<string>? mimeTypes))
            {
                return "Only PDF, TXT, DOC and DOCX staff documents are allowed.";
            }

            string contentType = file.ContentType.Split(';', 2)[0].Trim();

            if (!mimeTypes.Contains(contentType))
            {
                return $"The MIME type '{contentType}' does not match the '{extension}' file type.";
            }

            return null;
        }
    }
}
