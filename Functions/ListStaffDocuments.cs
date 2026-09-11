using Azure;
using CoffeeNChill.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace CoffeeNChill.Functions
{
    public class ListStaffDocuments
    {
        private readonly StaffDocumentService _staffDocumentService;

        public ListStaffDocuments(StaffDocumentService staffDocumentService)
        {
            _staffDocumentService = staffDocumentService;
        }

        [Function("ListStaffDocuments")]
        public async Task<IActionResult> Run(
            [HttpTrigger(
                AuthorizationLevel.Anonymous,
                "get",
                Route = "documents")] HttpRequest req)
        {
            try
            {
                var documents = await _staffDocumentService.ListAsync(
                    req.HttpContext.RequestAborted);

                return new OkObjectResult(documents);
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
                    error = "The staff documents could not be retrieved from Azure Files."
                })
                {
                    StatusCode = StatusCodes.Status502BadGateway
                };
            }
        }
    }
}
