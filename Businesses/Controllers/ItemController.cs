﻿using System.Security.Claims;
using instock_server_application.AwsS3.Services.Interfaces;
using instock_server_application.Businesses.Controllers.forms;
using instock_server_application.Businesses.Dtos;
using instock_server_application.Businesses.Services.Interfaces;
using instock_server_application.Util.Dto;
using Microsoft.AspNetCore.Mvc;

namespace instock_server_application.Businesses.Controllers; 

[ApiController]
[Route("/businesses/{businessId}")]
public class ItemController : ControllerBase {
    private readonly IItemService _itemService;
    private readonly IStorageService _storageService;
    
    public ItemController(IItemService itemService, IStorageService storageService) {
        _itemService = itemService;
        _storageService = storageService;
    }

    private UserAuthorisationDto GetUserAuthorisationDto() {
        string? currentUserId = User.FindFirstValue("Id") ?? null;
        string? currentUserBusinessId = User.FindFirstValue("BusinessId") ?? null;

        return new UserAuthorisationDto(currentUserId ?? "", currentUserBusinessId ?? "");
    }
    
    /// <summary>
    /// Function for getting all the items for a specific business, providing the currently logged in user has access
    /// </summary>
    /// <param name="businessIdModel"> The BusinessID to get all the items for </param>
    /// <returns> List of all the Items found, or an error message with a 404 status code </returns>
    [HttpGet]
    [Route("items")]
    public async Task<IActionResult> GetAllItems([FromRoute] string businessId) {
        
        // Get our current UserId and BusinessId to validate and pass to the business service
        string? currentUserId = User.FindFirstValue("Id") ?? null;
        string? currentUserBusinessId = User.FindFirstValue("BusinessId") ?? null;

        // Check there are no issues with the userId
        if (string.IsNullOrEmpty(currentUserId) | string.IsNullOrEmpty(currentUserBusinessId)) {
            return Unauthorized();
        }
        
        // Creating new userDto to pass into service
        UserDto currentUserDto = new UserDto(currentUserId, currentUserBusinessId);
        
        ListOfItemDto listOfItemDto = _itemService.GetItems(currentUserDto, businessId).Result;

        if (listOfItemDto.ErrorNotification.Errors.ContainsKey("otherErrors")) {
            if (listOfItemDto.ErrorNotification.Errors["otherErrors"].Contains(ListOfItemDto.ERROR_UNAUTHORISED)) {
                return Unauthorized();
            }
        }
        
        if (listOfItemDto.ListOfItems.Count <= 0) {
            return NotFound();
        }
        
        // Manually building the response object so we can keep deprecated fields in
        List<Dictionary<string, object>> returnListOfItems = new List<Dictionary<string, object>>();
        foreach (ItemDto item in listOfItemDto.ListOfItems) {
            returnListOfItems.Add(
                new Dictionary<string, object>(){
                    { nameof(ItemDto.SKU), item.SKU },
                    { nameof(ItemDto.BusinessId), item.BusinessId },
                    { nameof(ItemDto.Category), item.Category },
                    { nameof(ItemDto.Name), item.Name },
                    { "Stock", item.Stock.ToString() },
                    { nameof(ItemDto.TotalStock), item.TotalStock.ToString() },
                    { nameof(ItemDto.TotalOrders), item.TotalOrders.ToString() },
                    { nameof(ItemDto.AvailableStock), item.AvailableStock.ToString() ?? "" },
                    { "ImageUrl", item.ImageFilename != null ? _storageService.GetFilePresignedUrl("instock-item-images", item.ImageFilename ?? "").Message : "" },
                });
        }
        return Ok(returnListOfItems);
    }

    /// <summary>
    /// Add item for a specific business
    /// </summary>
    /// <param name="newItemForm"> Create item form </param>
    /// <param name="businessId"> Unique ID for the business the item needs to be added to</param>
    /// <returns> Item created, or error with relevant status code</returns>
    [HttpPost]
    [Route("items")]
    public async Task<IActionResult> CreateItem([FromForm] CreateItemForm newItemForm, [FromRoute] string businessId) {

        // Get our current UserId and BusinessId to validate and pass to the items service
        string? currentUserId = User.FindFirstValue("Id") ?? null;
        
        // Check there are no issues with the userId
        if (string.IsNullOrEmpty(currentUserId)) {
            return Unauthorized();
        }

        // Creating CreateItemDTO to pass the details to the service for processing
        CreateItemRequestDto createItemRequestDto = new CreateItemRequestDto(
            newItemForm.SKU,
            businessId, 
            newItemForm.Category, 
            newItemForm.Name, 
            newItemForm.Stock, 
            currentUserId,
            newItemForm.ImageFile
        );

        // Attempting to create new item
        ItemDto createdItemDto = await _itemService.CreateItem(createItemRequestDto);

        // If errors then return 401 with the error messages
        if (createdItemDto.ErrorNotification.HasErrors) {
            return new BadRequestObjectResult(createdItemDto.ErrorNotification);
        }
        
        // If not errors then return 201 with the URI and newly created object details
        string? createdItemUrl = Url.Action(controller: "item", action: nameof(GetItem), values:new
        {
            businesses=Url.RouteUrl("businesses"),
            businessId = createdItemDto.BusinessId,
            items=Url.RouteUrl("items"),
            itemId=createdItemDto.SKU
        }, protocol:Request.Scheme);
        
        return Created(createdItemUrl ?? string.Empty, new {
            sku = createdItemDto.SKU,
            businessId = createdItemDto.BusinessId,
            category = createdItemDto.Category,
            name = createdItemDto.Name,
            stock = createdItemDto.TotalStock,
            imageFilename = createdItemDto.ImageFilename
        });
    }
    
    [HttpGet]
    [Route("items/{itemId}")]
    public async Task<IActionResult> GetItem(string businessId, [FromRoute] string itemId) {
        UserAuthorisationDto userAuthorisationDto = GetUserAuthorisationDto();
        
        // Check there are no issues with the requesting user
        if (!userAuthorisationDto.IsValid()) {
            return Unauthorized();
        }

        ItemRequestDto itemRequestDto = new ItemRequestDto(itemId, businessId);

        ItemDetailsDto itemDetailsDto = await _itemService.GetItem(userAuthorisationDto, itemRequestDto);
        
        if (itemDetailsDto.ErrorNotification.HasErrors) {
            if (itemDetailsDto.ErrorNotification.Errors["otherErrors"].Contains(UserAuthorisationDto.USER_UNAUTHORISED_ERROR)) {
                return Unauthorized();
            }
            return new BadRequestObjectResult(itemDetailsDto);
        }

        return Ok(itemDetailsDto);
    }

    [HttpDelete]
    [Route("items/{itemId}")]
    public async Task<IActionResult> DeleteItem([FromRoute] string itemId, [FromRoute] string businessId) {
        DeleteItemDto result = _itemService.DeleteItem(new DeleteItemDto(itemId, User.FindFirstValue("BusinessId"), businessId)).Result;

        if (result.ErrorNotification.HasErrors) {
            if (result.ErrorNotification.Errors["otherErrors"].Contains(DeleteItemDto.USER_UNAUTHORISED_ERROR)) {
                return Unauthorized();
            }
            return new BadRequestObjectResult(result.ErrorNotification.Errors);
        }
        return Ok();
    }
    
    /// <summary>
    /// Function for getting all the categories for a specific business, providing the currently logged in user has access
    /// </summary>
    /// <param name="businessIdModel"> The BusinessID to get all the categories for </param>
    /// <returns> List of all the Categories found, or an error message with a 404 status code </returns>
    [HttpGet]
    [Route("categories")]
    public async Task<IActionResult> GetAllCategories([FromRoute] string businessId) {
        
        // Get our current UserId and BusinessId to validate and pass to the business service
        string? currentUserId = User.FindFirstValue("Id") ?? null;
        string? currentUserBusinessId = User.FindFirstValue("BusinessId") ?? null;

        // Check there are no issues with the userId
        if (string.IsNullOrEmpty(currentUserId) | string.IsNullOrEmpty(currentUserBusinessId)) {
            return Unauthorized();
        }

        List<Dictionary<string, string>>? categories = _itemService.GetCategories(new ValidateBusinessIdDto(currentUserBusinessId, businessId)).Result;

        if (categories == null) {
            return Unauthorized();
        } if (categories.Count == 0) {
            return NotFound();
        }

        return Ok(categories);
    }
}