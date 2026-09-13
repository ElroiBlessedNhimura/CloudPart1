using System.Net;
using CoffeeNChill.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

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
        public async Task<HttpResponseData> Run(
            [HttpTrigger(
                AuthorizationLevel.Anonymous,
                "get",
                Route = "documents")] HttpRequestData req)
        {
            var documents = await _staffDocumentService.ListAsync();
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(documents);
            return response;
        }
    }
}
