﻿using System.Security.Claims;
using instock_server_application.Businesses.Controllers.forms;
using instock_server_application.Businesses.Dtos;
using instock_server_application.Businesses.Models;
using instock_server_application.Businesses.Repositories.Interfaces;
using instock_server_application.Businesses.Services;
using instock_server_application.Businesses.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace instock_server_application.Businesses.Controllers; 

[ApiController]
[Route("/businesses/{businessId}/items/{itemSku}")]
public class ItemConnectionController : ControllerBase {
    
    private readonly IItemConnectionService _itemConnectionService;

        
    public ItemConnectionController(IItemConnectionService connectionService) {
        _itemConnectionService = connectionService;
    }
    
    [HttpGet]
    [Route("connect")]
    public async Task<IActionResult> ConnectItem([FromRoute] string businessId, [FromRoute] string itemSku, [FromBody] CreateItemConnectionForm createItemConnectionForm) {
        
        // Get our current UserId and BusinessId to validate and pass to the business service
        string? currentUserId = User.FindFirstValue("Id") ?? null;
        string? currentUserBusinessId = User.FindFirstValue("BusinessId") ?? null;
    
        // Check there are no issues with the userId
        if (string.IsNullOrEmpty(currentUserId) | string.IsNullOrEmpty(currentUserBusinessId)) {
            return Unauthorized();
        }
    
        UserAuthorisationDto userAuthDto = new UserAuthorisationDto(currentUserId!, currentUserBusinessId!);
        ItemConnectionRequestDto itemConnectionDtoRequest =
            new ItemConnectionRequestDto(businessId, itemSku, createItemConnectionForm.PlatformName.ToLower(), createItemConnectionForm.PlatformItemSku);
        
        ItemConnectionsDto itemConnectionsDtoReturned = await _itemConnectionService.ConnectItem(userAuthDto, itemConnectionDtoRequest);

        if (itemConnectionsDtoReturned.ErrorNotification.HasErrors) {
            return BadRequest(itemConnectionsDtoReturned);
        }
        
        return Ok(itemConnectionsDtoReturned);
    }
}
