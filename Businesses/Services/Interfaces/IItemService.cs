﻿using instock_server_application.Businesses.Dtos;
using instock_server_application.Shared.Dto;

namespace instock_server_application.Businesses.Services.Interfaces;

public interface IItemService {
    public Task<List<Dictionary<string, string>>?> GetItems(UserDto userDto, string businessId);
    
    Task<ItemDto> CreateItem(CreateItemRequestDto newItemRequestDto);

    Task<DeleteItemDto> DeleteItem(DeleteItemDto deleteItemDto);
    
    public Task<List<Dictionary<string, string>>?> GetCategories(ValidateBusinessIdDto validateBusinessIdDto);
}