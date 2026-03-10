using System.Collections.Generic;
using System.Threading.Tasks;
using CulinaryCommand.Inventory.DTOs;

namespace CulinaryCommand.Inventory.Services.Interfaces
{
    public interface IInventoryManagementService
    {
        Task<List<InventoryItemDTO>> GetAllItemsAsync();
        Task<List<InventoryItemDTO>> GetItemsByLocationAsync(int locationId);
        Task<List<string>> GetCategoriesByLocationAsync(int locationId);
        Task<InventoryItemDTO> AddItemAsync(CreateIngredientDTO dto);
        Task<bool> DeleteItemAsync(int id);
        Task<InventoryItemDTO?> UpdateItemAsync(InventoryItemDTO dto);
    }
}