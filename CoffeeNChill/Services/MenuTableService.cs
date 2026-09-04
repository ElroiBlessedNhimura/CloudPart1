using Azure.Data.Tables;
using CoffeeNChill.Functions.Models;
using CoffeeNChill.Functions.Models;

namespace CoffeeNChill.Functions.Services
{
    public class MenuTableService
    {
        private readonly TableClient _tableClient;

        public MenuTableService()
        {
            _tableClient = new TableClient(
                "UseDevelopmentStorage=true",
                "MenuItems");

            _tableClient.CreateIfNotExists();
        }

        // CREATE
        public async Task AddMenuItemAsync(MenuItem item)
        {
            await _tableClient.AddEntityAsync(item);
        }

        // READ ALL
        public async Task<List<MenuItem>> GetAllMenuItemsAsync()
        {
            var items = new List<MenuItem>();

            await foreach (var item in _tableClient.QueryAsync<MenuItem>())
            {
                items.Add(item);
            }

            return items;
        }
    }
}
