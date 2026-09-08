using System.Net;
using CoffeeNChill.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace CoffeeNChill.Functions
{
    public class GetAllMenuItems
    {
        private readonly MenuTableService _menuTableService;
        private readonly ILogger<GetAllMenuItems> _logger;

        public GetAllMenuItems(
            MenuTableService menuTableService,
            ILogger<GetAllMenuItems> logger)
        {
            _menuTableService = menuTableService;
            _logger = logger;
        }

        [Function("GetAllMenuItems")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(
                AuthorizationLevel.Anonymous,
                "get",
                Route = "menu")] HttpRequestData req)
        {
            _logger.LogInformation("Retrieving all menu items.");

            var entities = await _menuTableService.GetAllMenuItemsAsync();

            var menuItems = entities.Select(entity => new
            {
                category = entity.PartitionKey,
                id = entity.RowKey,
                name = entity.Name,
                description = entity.Description,
                price = entity.Price,
                isAvailable = entity.IsAvailable
            });

            
            var response = req.CreateResponse(HttpStatusCode.OK);

            await response.WriteAsJsonAsync(menuItems);

            return response;
        }
    }
}