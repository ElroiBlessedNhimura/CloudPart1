using Azure;
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

        // READ BY CATEGORY
        public async Task<List<MenuItemEntity>> GetByCategoryAsync(string category)
        {
            var items = new List<MenuItemEntity>();

            await foreach (var item in _tableClient.QueryAsync<MenuItemEntity>(
                filter: e => e.PartitionKey == category))
            {
                items.Add(item);
            }

            return items;
        }

        // READ ONE (used by update/delete)
        public async Task<MenuItemEntity?> GetMenuItemAsync(string category, string id)
        {
            try
            {
                var response = await _tableClient.GetEntityAsync<MenuItemEntity>(
                    category, id);

                return response.Value;
            }
            catch (RequestFailedException ex)
                when (ex.Status == 404)
            {
                return null;
            }
        }

        // UPDATE
        public async Task<MenuItemEntity?> UpdateMenuItemAsync(
            string category, string id, double? price, bool? isAvailable)
        {
            var existing = await GetMenuItemAsync(category, id);

            if (existing == null) return null;

            if (price.HasValue) existing.Price = price.Value;
            if (isAvailable.HasValue) existing.IsAvailable = isAvailable.Value;

            await _tableClient.UpdateEntityAsync(
                existing, existing.ETag, TableUpdateMode.Replace);

            return existing;
        }

        // DELETE
        public async Task<bool> DeleteMenuItemAsync(string category, string id)
        {
            try
            {
                await _tableClient.DeleteEntityAsync(category, id);
                return true;
            }
            catch (RequestFailedException ex)
                when (ex.Status == 404)
            {
                return false;
            }
        }
    }
}