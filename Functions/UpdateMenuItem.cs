using System.Net;
using System.Text.Json;
using CoffeeNChill.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace CoffeeNChill.Functions
{
    public class UpdateMenuItem
    {
        private readonly MenuTableService _menuTableService;
        private readonly ILogger<UpdateMenuItem> _logger;

        public UpdateMenuItem(
            MenuTableService menuTableService,
            ILogger<UpdateMenuItem> logger)
        {
            _menuTableService = menuTableService;
            _logger = logger;
        }

        private class UpdateRequest
        {
            public double? Price { get; set; }
            public bool? IsAvailable { get; set; }
        }

        [Function("UpdateMenuItem")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(
                AuthorizationLevel.Anonymous,
                "put",
                Route = "menu/{category}/{id}")]
            HttpRequestData req,
            string category,
            string id)
        {
            _logger.LogInformation(
                "Updating menu item {Category}/{Id}", category, id);

            // VALIDATION: category and id required
            if (string.IsNullOrWhiteSpace(category) ||
                string.IsNullOrWhiteSpace(id))
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteStringAsync(
                    "Category and ID are required.");
                return badRequest;
            }

            // Parse body
            UpdateRequest? update;
            try
            {
                update = await JsonSerializer.DeserializeAsync<UpdateRequest>(
                    req.Body,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (JsonException)
            {
                var badJson = req.CreateResponse(HttpStatusCode.BadRequest);
                await badJson.WriteStringAsync("Invalid JSON body.");
                return badJson;
            }

            if (update == null)
            {
                var badBody = req.CreateResponse(HttpStatusCode.BadRequest);
                await badBody.WriteStringAsync("Request body is required.");
                return badBody;
            }

            // VALIDATION: price must be positive if provided
            if (update.Price.HasValue && update.Price.Value <= 0)
            {
                var badPrice = req.CreateResponse(HttpStatusCode.BadRequest);
                await badPrice.WriteStringAsync(
                    "Price must be greater than 0.");
                return badPrice;
            }

            // VALIDATION: at least one field to update
            if (!update.Price.HasValue && !update.IsAvailable.HasValue)
            {
                var noFields = req.CreateResponse(HttpStatusCode.BadRequest);
                await noFields.WriteStringAsync(
                    "Provide 'price' and/or 'isAvailable' to update.");
                return noFields;
            }

            var updated = await _menuTableService.UpdateMenuItemAsync(
                category, id, update.Price, update.IsAvailable);

            if (updated == null)
            {
                var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                await notFound.WriteStringAsync(
                    $"Menu item {category}/{id} not found.");
                return notFound;
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(new
            {
                message = "Menu item updated successfully.",
                item = new
                {
                    category = updated.PartitionKey,
                    id = updated.RowKey,
                    name = updated.Name,
                    description = updated.Description,
                    price = updated.Price,
                    isAvailable = updated.IsAvailable
                }
            });

            return response;
        }
    }
}