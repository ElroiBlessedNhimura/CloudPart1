using System.Net;
using System.Text.Json;
using CoffeeNChill.Models;
using CoffeeNChill.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace CoffeeNChill.Functions
{
    public class CreateMenuItem
    {
        private readonly MenuTableService _menuTableService;
        private readonly ILogger<CreateMenuItem> _logger;

        public CreateMenuItem(
            MenuTableService menuTableService,
            ILogger<CreateMenuItem> logger)
        {
            _menuTableService = menuTableService;
            _logger = logger;
        }

        [Function("CreateMenuItem")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(
                AuthorizationLevel.Anonymous,
                "post",
                Route = "menu")] HttpRequestData req)
        {
            _logger.LogInformation("Creating a new menu item.");

            // Reads JSON from the request body
            MenuItem? menuItem;

            try
            {
                menuItem = await JsonSerializer.DeserializeAsync<MenuItem>(
                    req.Body,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
            }
            catch (JsonException)
            {
                var badJsonResponse = req.CreateResponse(
                    HttpStatusCode.BadRequest);

                await badJsonResponse.WriteStringAsync(
                    "Invalid JSON request body.");

                return badJsonResponse;
            }

            // Checks if the request body is empty
            if (menuItem == null)
            {
                var badRequestResponse = req.CreateResponse(
                    HttpStatusCode.BadRequest);

                await badRequestResponse.WriteStringAsync(
                    "Request body is required.");

                return badRequestResponse;
            }

            // Save the menu item to Azure Table Storage
            var entity = await _menuTableService.AddMenuItemAsync(menuItem);

            // Return 201 Created
            var response = req.CreateResponse(
                HttpStatusCode.Created);

            await response.WriteAsJsonAsync(new
            {
                message = "Menu item created successfully.",

                item = new
                {
                    category = entity.PartitionKey,
                    id = entity.RowKey,
                    name = entity.Name,
                    description = entity.Description,
                    price = entity.Price,
                    isAvailable = entity.IsAvailable
                }
            });

            return response;
        }
    }
}