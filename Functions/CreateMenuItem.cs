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

        // ---------------------------------------------------------------------
        // Code Attribution
        // Constructor dependency injection pattern adapted from:
        // Microsoft Learn – Dependency injection in .NET Azure Functions
        // https://learn.microsoft.com/en-us/azure/azure-functions/functions-dotnet-dependency-injection
        // ---------------------------------------------------------------------
        public CreateMenuItem(
            MenuTableService menuTableService,
            ILogger<CreateMenuItem> logger)
        {
            _menuTableService = menuTableService;
            _logger = logger;
        }

        // ---------------------------------------------------------------------
        // Code Attribution
        // Azure Function HTTP trigger with custom route adapted from:
        // Microsoft Learn – Azure Functions .NET isolated worker guide
        // https://learn.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide
        // ---------------------------------------------------------------------
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

            // -----------------------------------------------------------------
            // Code Attribution
            // JSON deserialization from HttpRequestData body adapted from:
            // https://learn.microsoft.com/en-us/dotnet/api/system.text.json.jsonserializer.deserializeasync
            // and Stack Overflow:
            // https://stackoverflow.com/questions/71430256/how-to-read-json-body-from-httprequestdata
            // -----------------------------------------------------------------
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
            // -----------------------------------------------------------------
            // Code Attribution
            // Null-check and BadRequest response pattern adapted from:
            // Microsoft Learn – Create and use HTTP responses in Azure Functions
            // https://learn.microsoft.com/en-us/azure/azure-functions/functions-reference
            // -----------------------------------------------------------------
            if (menuItem == null)
            {
                var badRequestResponse = req.CreateResponse(
                    HttpStatusCode.BadRequest);

                await badRequestResponse.WriteStringAsync(
                    "Request body is required.");

                return badRequestResponse;
            }

            // VALIDATION: Category, Id, and Name are required
            // -----------------------------------------------------------------
            // Code Attribution
            // Input validation pattern (IsNullOrWhiteSpace checks) adapted from:
            // Microsoft Learn – String.IsNullOrWhiteSpace Method
            // https://learn.microsoft.com/en-us/dotnet/api/system.string.isnullorwhitespace
            // -----------------------------------------------------------------
            if (string.IsNullOrWhiteSpace(menuItem.Category))
            {
                var badCategory = req.CreateResponse(HttpStatusCode.BadRequest);
                await badCategory.WriteStringAsync("Category is required.");
                return badCategory;
            }

            if (string.IsNullOrWhiteSpace(menuItem.Id))
            {
                var badId = req.CreateResponse(HttpStatusCode.BadRequest);
                await badId.WriteStringAsync("Id is required.");
                return badId;
            }

            if (string.IsNullOrWhiteSpace(menuItem.Name))
            {
                var badName = req.CreateResponse(HttpStatusCode.BadRequest);
                await badName.WriteStringAsync("Name is required.");
                return badName;
            }

            // VALIDATION: Price must be greater than 0
            // -----------------------------------------------------------------
            // Code Attribution
            // Numeric range validation pattern adapted from:
            // Microsoft Learn – C# comparison operators
            // https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/comparison-operators
            // -----------------------------------------------------------------
            if (menuItem.Price <= 0)
            {
                var badPrice = req.CreateResponse(HttpStatusCode.BadRequest);
                await badPrice.WriteStringAsync("Price must be greater than 0.");
                return badPrice;
            }

            // -----------------------------------------------------------------
            // Code Attribution
            // Saving entity to Azure Table Storage adapted from:
            // Microsoft Learn – Azure Tables client library for .NET
            // https://learn.microsoft.com/en-us/dotnet/api/overview/azure/data.tables-readme
            // -----------------------------------------------------------------
            var entity = await _menuTableService.AddMenuItemAsync(menuItem);

            // Return 201 Created
            // -----------------------------------------------------------------
            // Code Attribution
            // HTTP 201 Created response and JSON payload pattern adapted from:
            // Microsoft Learn – Create and use HTTP responses in Azure Functions
            // https://learn.microsoft.com/en-us/azure/azure-functions/functions-reference
            // -----------------------------------------------------------------
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