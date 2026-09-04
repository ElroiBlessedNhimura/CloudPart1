using System.Net;
using System.Text.Json;
using CoffeeNChill.Functions.Models;
using CoffeeNChill.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace CoffeeNChill.Functions.Functions
{
    public class MenuFunctions
    {
        private readonly MenuTableService _menuService;

        public MenuFunctions(MenuTableService menuService)
        {
            _menuService = menuService;
        }

        // POST /api/menu
        [Function("CreateMenuItem")]
        public async Task<HttpResponseData> CreateMenuItem(
            [HttpTrigger(
                AuthorizationLevel.Anonymous,
                "post",
                Route = "menu")]
            HttpRequestData req)
        {
            try
            {
                var menuItem =
                    await JsonSerializer.DeserializeAsync<MenuItem>(
                        req.Body,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                if (menuItem == null)
                {
                    var badResponse =
                        req.CreateResponse(HttpStatusCode.BadRequest);

                    await badResponse.WriteStringAsync(
                        "Invalid menu item data.");

                    return badResponse;
                }

                // Basic validation
                if (string.IsNullOrWhiteSpace(menuItem.PartitionKey) ||
                    string.IsNullOrWhiteSpace(menuItem.RowKey) ||
                    string.IsNullOrWhiteSpace(menuItem.Name))
                {
                    var badResponse =
                        req.CreateResponse(HttpStatusCode.BadRequest);

                    await badResponse.WriteStringAsync(
                        "PartitionKey, RowKey and Name are required.");

                    return badResponse;
                }

                if (menuItem.Price < 0)
                {
                    var badResponse =
                        req.CreateResponse(HttpStatusCode.BadRequest);

                    await badResponse.WriteStringAsync(
                        "Price cannot be negative.");

                    return badResponse;
                }

                await _menuService.AddMenuItemAsync(menuItem);

                var response =
                    req.CreateResponse(HttpStatusCode.Created);

                await response.WriteAsJsonAsync(menuItem);

                return response;
            }
            catch (Exception ex)
            {
                var response =
                    req.CreateResponse(
                        HttpStatusCode.InternalServerError);

                await response.WriteStringAsync(
                    $"Error creating menu item: {ex.Message}");

                return response;
            }
        }

        // GET /api/menu
        [Function("GetAllMenuItems")]
        public async Task<HttpResponseData> GetAllMenuItems(
            [HttpTrigger(
                AuthorizationLevel.Anonymous,
                "get",
                Route = "menu")]
            HttpRequestData req)
        {
            try
            {
                var menuItems =
                    await _menuService.GetAllMenuItemsAsync();

                var response =
                    req.CreateResponse(HttpStatusCode.OK);

                await response.WriteAsJsonAsync(menuItems);

                return response;
            }
            catch (Exception ex)
            {
                var response =
                    req.CreateResponse(
                        HttpStatusCode.InternalServerError);

                await response.WriteStringAsync(
                    $"Error retrieving menu items: {ex.Message}");

                return response;
            }
        }
    }
}