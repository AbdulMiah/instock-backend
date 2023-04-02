﻿using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using instock_server_application.Businesses.Dtos;
using instock_server_application.Businesses.Models;
using instock_server_application.Businesses.Repositories.Interfaces;
using Microsoft.AspNetCore.Connections;

namespace instock_server_application.Businesses.Repositories; 

public class ItemRepo : IItemRepo{
    private readonly IAmazonDynamoDB _client;
    private readonly IDynamoDBContext _context;

    public ItemRepo(IAmazonDynamoDB client, IDynamoDBContext context) {
        _client = client;
        _context = context;
    }
    
    public async Task<List<ItemDto>> GetAllItems(string businessId) {
        
        // Get List of items
        List<Item> listOfItemModel = await _context.ScanAsync<Item>(
            new [] {
                Item.ByBusinessId(businessId)
            }).GetRemainingAsync();

        // Convert list of items
        List<ItemDto> listOfItemDto = new List<ItemDto>();
        
        foreach (Item itemModel in listOfItemModel) {
            listOfItemDto.Add(
                new ItemDto(
                    sku: itemModel.SKU,
                    businessId: itemModel.BusinessId,
                    category: itemModel.Category,
                    name: itemModel.Name,
                    stock: itemModel.GetStock()));
        }
        
        return listOfItemDto;
    }
    
    public async Task<ItemDto> SaveNewItem(StoreItemDto itemToSaveDto) {
        
        // Checking the Business Name is valid
        if (string.IsNullOrEmpty(itemToSaveDto.Name)) {
            throw new NullReferenceException("The Business Name cannot be null or empty.");
        }
        
        // Save the new item
        Item itemModel = new Item(
            itemToSaveDto.SKU, itemToSaveDto.BusinessId, itemToSaveDto.Category, itemToSaveDto.Name, itemToSaveDto.Stock);
        await _context.SaveAsync(itemModel);

        ItemDto createdItemDto = new ItemDto(
            itemModel.SKU, 
            itemModel.BusinessId, 
            itemModel.Category,
            itemModel.Name,
            itemModel.GetStock());
        
        return createdItemDto;
    }
    
    public async Task<bool> IsNameInUse(CreateItemRequestDto createItemRequestDto) {
        var response = await _context.ScanAsync<Item>(
            new[] {
                Item.ByBusinessName(createItemRequestDto.Name)
            }).GetRemainingAsync();
        
        return response.Count > 0;
    }
    
    public async Task<bool> IsSkuInUse(string sku) {
        var response = await _context.ScanAsync<Item>(
            new[] {
                Item.ByBusinessSku(sku)
            }).GetRemainingAsync();
    
        return response.Count > 0;
    }

    public void Delete(DeleteItemDto deleteItemDto) {
        Item item = new Item(deleteItemDto.ItemId, deleteItemDto.BusinessId);
        _context.DeleteAsync(item);
    }
    
    public async Task<List<Dictionary<string, AttributeValue>>> GetAllCategories(ValidateBusinessIdDto validateBusinessIdDto) {
        var request = new ScanRequest
        {
            TableName = Item.TableName,
            ProjectionExpression = "Category",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue> {
                {":Id", new AttributeValue(validateBusinessIdDto.BusinessId)}
            },
            FilterExpression = "BusinessId = :Id",
        };
    
        var response = await _client.ScanAsync(request);
    
        var categories = new HashSet<string>();
        var categoryList = new List<Dictionary<string, AttributeValue>>();
        foreach (var item in response.Items) {
            var categoryValue = item["Category"];
            var category = categoryValue.S;
            if (categories.Add(category)) {
                categoryList.Add(new Dictionary<string, AttributeValue> {
                    {"Category", categoryValue}
                });
            }
        }
    
        return categoryList;
    }
    
    public async Task<ItemDto> SaveExistingItem(StoreItemDto itemToSaveDto) {
                
        // Checking the Item SKU is valid
        if (string.IsNullOrEmpty(itemToSaveDto.SKU)) {
            throw new NullReferenceException("The Item SKU cannot be null or empty.");
        }
        
        // Checking the Item Name is valid
        if (string.IsNullOrEmpty(itemToSaveDto.Name)) {
            throw new NullReferenceException("The Item Name cannot be null or empty.");
        }
        
        // Check if the item already exists so we know we are updating one
        Item existingItem = await _context.LoadAsync<Item>(itemToSaveDto.SKU, itemToSaveDto.BusinessId);

        if (existingItem == null) {
            throw new NullReferenceException("The Item you are trying to update does not exist");
        }
        
        // Save the new updated item
        Item itemModel = new Item(
            itemToSaveDto.SKU, itemToSaveDto.BusinessId, itemToSaveDto.Category, itemToSaveDto.Name, itemToSaveDto.Stock);
        await _context.SaveAsync(itemModel);
        
        // Return the updated Items details
        ItemDto updatedItemDto = new ItemDto(
            itemModel.SKU, 
            itemModel.BusinessId, 
            itemModel.Category,
            itemModel.Name,
            itemModel.GetStock());
        
        return updatedItemDto;
    }

    public async Task<StockUpdateDto> SaveStockUpdate(StoreStockUpdateDto storeStockUpdateDto) {
        // Validating details
        if (string.IsNullOrEmpty(storeStockUpdateDto.BusinessId)) {
            throw new NullReferenceException("The stock update business ID cannot be null.");
        }
        if (string.IsNullOrEmpty(storeStockUpdateDto.ItemSku)) {
            throw new NullReferenceException("The stock update item ID cannot be null.");
        }

        // Getting the existing items stock updates
        ItemStockUpdateModel existingStockUpdates =
            await _context.LoadAsync<ItemStockUpdateModel>(storeStockUpdateDto.ItemSku, storeStockUpdateDto.BusinessId);

        // Adding to the existing stock updates
        existingStockUpdates.AddStockUpdateDetails(storeStockUpdateDto.ChangeStockAmountBy, storeStockUpdateDto.ReasonForChange, storeStockUpdateDto.DateTimeAdded);

        // Saving all of the updates
        await _context.SaveAsync(existingStockUpdates);

        // Returning the new object that was saved
        StockUpdateDto stockUpdateDto =
            new StockUpdateDto(storeStockUpdateDto.ChangeStockAmountBy, storeStockUpdateDto.ReasonForChange, storeStockUpdateDto.DateTimeAdded);
        
        return stockUpdateDto;
    }

    public async Task<ItemDto?> GetItem(string businessId, string itemSku) {
        // Validating details
        if (string.IsNullOrEmpty(businessId)) {
            throw new NullReferenceException("The stock update business ID cannot be null.");
        }
        if (string.IsNullOrEmpty(itemSku)) {
            throw new NullReferenceException("The stock update item ID cannot be null.");
        }

        // Getting the existing item
        Item item = await _context.LoadAsync<Item>(itemSku, businessId);

        // If the item wasn't in the database then return null
        if (item == null) {
            return null;
        }
        
        // Returning the item details from the database
        ItemDto itemDto = new ItemDto(item.SKU, item.BusinessId, item.Category, item.Name, item.GetStock());
        return itemDto;
    }
    
    
    public async Task<ItemOrderDto> SaveItemOrder(StoreItemOrderDto storeItemOrderDto) {
        // Validating details
        if (string.IsNullOrEmpty(storeItemOrderDto.BusinessId)) {
            throw new NullReferenceException("The stock update business ID cannot be null.");
        }
        if (string.IsNullOrEmpty(storeItemOrderDto.ItemSku)) {
            throw new NullReferenceException("The stock update item ID cannot be null.");
        }

        // Getting the existing items stock updates
        ItemOrdersModel existingItemOrders =
            await _context.LoadAsync<ItemOrdersModel>(storeItemOrderDto.ItemSku, storeItemOrderDto.BusinessId);

        // Adding to the existing stock updates
        existingItemOrders.AddItemOrderDetails(storeItemOrderDto.AmountOrdered, storeItemOrderDto.DateTimeAdded);

        // Saving all of the updates
        await _context.SaveAsync(existingItemOrders);

        // Returning the new object that was saved
        ItemOrderDto itemOrderDto =
            new ItemOrderDto(storeItemOrderDto.AmountOrdered, storeItemOrderDto.DateTimeAdded);
        
        return itemOrderDto;
    }
}