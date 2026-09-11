using System.Net;
using CoffeeNChill.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace CoffeeNChill.Functions
{
    public class DeleteMenuItem
    {
        private readonly MenuTableService _menuTableService;
        private readonly ILogger<DeleteMenuItem> _logger;

        public DeleteMenuItem(
            MenuTableService menuTableService,
            ILogger<DeleteMenuItem> logger)
        {
            _menuTableService = menuTableService;
            _logger = logger;
        }

        [Function("DeleteMenuItem")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(
                AuthorizationLevel.Anonymous,
                "delete",
                Route = "menu/{category}/{id}")]
            HttpRequestData req,
            string category,
            string id)
        {
            _logger.LogInformation(
                "Deleting menu item {Category}/{Id}", category, id);

            if (string.IsNullOrWhiteSpace(category) ||
                string.IsNullOrWhiteSpace(id))
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteStringAsync(
                    "Category and ID are required.");
                return badRequest;
            }

            var deleted = await _menuTableService
                .DeleteMenuItemAsync(category, id);

            if (!deleted)
            {
                var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                await notFound.WriteStringAsync(
                    $"Menu item {category}/{id} not found.");
                return notFound;
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                message = "Menu item deleted successfully."
            });

            return response;
        }
    }
}