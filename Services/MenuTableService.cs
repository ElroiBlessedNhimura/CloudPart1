using Azure.Data.Tables;
using CoffeeNChill.Models;

namespace CoffeeNChill.Services
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
        public async Task<MenuItemEntity> AddMenuItemAsync(MenuItem item)
        {
            var entity = new MenuItemEntity
            {
                PartitionKey = item.Category,
                RowKey = item.Id,
                Name = item.Name,
                Description = item.Description,
                Price = item.Price,
                IsAvailable = item.IsAvailable
            };

            await _tableClient.AddEntityAsync(entity);

            return entity;
        }

        // READ ALL
        public async Task<List<MenuItemEntity>> GetAllMenuItemsAsync()
        {
            var items = new List<MenuItemEntity>();

            await foreach (var item in _tableClient.QueryAsync<MenuItemEntity>())
            {
                items.Add(item);
            }

            return items;
        }
    }
}