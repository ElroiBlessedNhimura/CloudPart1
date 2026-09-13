using System.Net;
using CoffeeNChill.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

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
        public async Task<HttpResponseData> Run(
            [HttpTrigger(
                AuthorizationLevel.Anonymous,
                "get",
                Route = "documents/download/{fileName}")] HttpRequestData req,
            string fileName)
        {
            var document = await _staffDocumentService.DownloadAsync(fileName);

            if (document == null)
            {
                var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                await notFound.WriteAsJsonAsync(new { message = "Document not found." });
                return notFound;
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", document.Value.ContentType);
            response.Headers.Add("Content-Disposition", $"attachment; filename=\"{Path.GetFileName(fileName)}\"");
            await document.Value.Content.CopyToAsync(response.Body);
            await document.Value.Content.DisposeAsync();
            return response;
        }
    }
}
