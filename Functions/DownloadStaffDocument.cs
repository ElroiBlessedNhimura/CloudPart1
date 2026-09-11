using Azure;
using CoffeeNChill.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace CoffeeNChill.Functions
{
    public class DownloadStaffDocument
    {
        private readonly StaffDocumentService _staffDocumentService;

        public DownloadStaffDocument(StaffDocumentService staffDocumentService)
        {
            _staffDocumentService = staffDocumentService;
        }

        [Function("DownloadStaffDocument")]
        public async Task<IActionResult> Run(
            [HttpTrigger(
                AuthorizationLevel.Anonymous,
                "get",
                Route = "documents/download/{fileName}")] HttpRequest req,
            string fileName)
        {
            try
            {
                var download = await _staffDocumentService.DownloadAsync(
                    fileName,
                    req.HttpContext.RequestAborted);

                if (download == null)
                {
                    return new NotFoundObjectResult(new
                    {
                        error = "The requested staff document was not found."
                    });
                }

                return new FileStreamResult(download.Value.Content, download.Value.ContentType)
                {
                    FileDownloadName = download.Value.FileName
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
                    error = "The staff document could not be downloaded from Azure Files."
                })
                {
                    StatusCode = StatusCodes.Status502BadGateway
                };
            }
        }
    }
}
