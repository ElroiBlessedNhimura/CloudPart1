using System.Net;
using CoffeeNChill.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace CoffeeNChill.Functions
{
    public class GetByCategory
    {
        private readonly MenuTableService _menuTableService;
        private readonly ILogger<GetByCategory> _logger;

        public GetByCategory(
            MenuTableService menuTableService,
            ILogger<GetByCategory> logger)
        {
            _menuTableService = menuTableService;
            _logger = logger;
        }

        [Function("GetByCategory")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(
                AuthorizationLevel.Anonymous,
                "get",
                Route = "menu/category/{category}")]
            HttpRequestData req,
            string category)
        {
            _logger.LogInformation(
                "Retrieving menu items for category: {Category}", category);

            // VALIDATION: category required
            if (string.IsNullOrWhiteSpace(category))
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteStringAsync(
                    "Category is required.");
                return badRequest;
            }

            var entities = await _menuTableService
                .GetByCategoryAsync(category);

            var result = entities.Select(e => new
            {
                category = e.PartitionKey,
                id = e.RowKey,
                name = e.Name,
                description = e.Description,
                price = e.Price,
                isAvailable = e.IsAvailable
            });

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(result);
            return response;
        }
    }
}