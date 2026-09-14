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

        // ---------------------------------------------------------------------
        // Code Attribution
        // Constructor dependency injection pattern adapted from:
        // Microsoft Learn – Dependency injection in .NET Azure Functions
        // https://learn.microsoft.com/en-us/azure/azure-functions/functions-dotnet-dependency-injection
        // ---------------------------------------------------------------------
        public GetByCategory(
            MenuTableService menuTableService,
            ILogger<GetByCategory> logger)
        {
            _menuTableService = menuTableService;
            _logger = logger;
        }

        // ---------------------------------------------------------------------
        // Code Attribution
        // Azure Function HTTP trigger with route parameter adapted from:
        // Microsoft Learn – Azure Functions .NET isolated worker guide
        // https://learn.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide
        // and:
        // Microsoft Learn – Azure Functions HTTP trigger route parameters
        // https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-http-webhook-trigger
        // ---------------------------------------------------------------------
        [Function("GetByCategory")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(
                AuthorizationLevel.Anonymous,
                "get",
                Route = "menu/category/{category}")]
            HttpRequestData req,
            string category)
        {
            // -----------------------------------------------------------------
            // Code Attribution
            // Structured logging with ILogger adapted from:
            // Microsoft Learn – Logging in .NET and Azure Functions
            // https://learn.microsoft.com/en-us/dotnet/core/extensions/logging
            // -----------------------------------------------------------------
            _logger.LogInformation(
                "Retrieving menu items for category: {Category}", category);

            // VALIDATION: category required
            // -----------------------------------------------------------------
            // Code Attribution
            // Input validation pattern (IsNullOrWhiteSpace check) adapted from:
            // Microsoft Learn – String.IsNullOrWhiteSpace Method
            // https://learn.microsoft.com/en-us/dotnet/api/system.string.isnullorwhitespace
            // -----------------------------------------------------------------
            if (string.IsNullOrWhiteSpace(category))
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteStringAsync(
                    "Category is required.");
                return badRequest;
            }

            // -----------------------------------------------------------------
            // Code Attribution
            // Querying entities by partition key from Azure Table Storage adapted from:
            // Microsoft Learn – Azure Tables client library for .NET
            // https://learn.microsoft.com/en-us/dotnet/api/overview/azure/data.tables-readme
            // -----------------------------------------------------------------
            var entities = await _menuTableService
                .GetByCategoryAsync(category);

            // -----------------------------------------------------------------
            // Code Attribution
            // Projecting entities into an anonymous object using LINQ Select adapted from:
            // Microsoft Learn – Language Integrated Query (LINQ) Select
            // https://learn.microsoft.com/en-us/dotnet/api/system.linq.enumerable.select
            // -----------------------------------------------------------------
            var result = entities.Select(e => new
            {
                category = e.PartitionKey,
                id = e.RowKey,
                name = e.Name,
                description = e.Description,
                price = e.Price,
                isAvailable = e.IsAvailable
            });

            // -----------------------------------------------------------------
            // Code Attribution
            // HTTP 200 OK response with JSON payload adapted from:
            // Microsoft Learn – Create and use HTTP responses in Azure Functions
            // https://learn.microsoft.com/en-us/azure/azure-functions/functions-reference
            // -----------------------------------------------------------------
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(result);
            return response;
        }
    }
}