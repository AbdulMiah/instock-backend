﻿using System.Security.Claims;
using instock_server_application.AwsS3.Services.Interfaces;
using instock_server_application.Businesses.Dtos;
using instock_server_application.Businesses.Services.Interfaces;
using instock_server_application.Util.Dto;
using Microsoft.AspNetCore.Mvc;

namespace instock_server_application.Businesses.Controllers; 

[ApiController]
[Route("/milestones")]
public class MilestoneController : ControllerBase {
    private readonly IMilestoneService _milestoneService;
    private readonly IStorageService _storageService;

    public MilestoneController(IMilestoneService milestoneService, IStorageService storageService) {
        _milestoneService = milestoneService;
        _storageService = storageService;
    }
    
    [HttpGet]
    [Route("{businessId}")]
    public async Task<IActionResult> GetMilestones([FromRoute] string businessId) {
        // Get our current UserId and BusinessId to validate and pass to the business service
        string? currentUserId = User.FindFirstValue("Id") ?? null;
        string? currentUserBusinessId = User.FindFirstValue("BusinessId") ?? null;

        // Check there are no issues with the userId
        if (string.IsNullOrEmpty(currentUserId) | string.IsNullOrEmpty(currentUserBusinessId)) {
            return Unauthorized();
        }
        
        // Creating new userDto to pass into service
        UserDto currentUserDto = new UserDto(currentUserId, currentUserBusinessId);
        
        ListOfMilestonesDto listOfMilestonesDto = _milestoneService.GetAllMilestones(currentUserDto, businessId).Result;

        if (listOfMilestonesDto.ErrorNotification.Errors.ContainsKey("otherErrors")) {
            if (listOfMilestonesDto.ErrorNotification.Errors["otherErrors"].Contains(ListOfItemDto.ERROR_UNAUTHORISED)) {
                return Unauthorized();
            }
        }
        
        if (listOfMilestonesDto.ListOfMilestones.Count <= 0) {
            return NotFound();
        }
        
        List<Dictionary<string, object>> returnListOfMilestones = new List<Dictionary<string, object>>();
        foreach (MilestoneDto milestone in listOfMilestonesDto.ListOfMilestones) {
            returnListOfMilestones.Add(
                new Dictionary<string, object>(){
                    { nameof(MilestoneDto.MilestoneId), milestone.MilestoneId },
                    { nameof(MilestoneDto.BusinessId), milestone.BusinessId },
                    { nameof(MilestoneDto.ItemSku), milestone.ItemSku },
                    { nameof(MilestoneDto.ItemName), milestone.ItemName },
                    { "ImageUrl", milestone.ImageFilename != null ? _storageService.GetFilePresignedUrl("instock-item-images", milestone.ImageFilename ?? "").Message : "" },
                    { nameof(MilestoneDto.TotalSales), milestone.TotalSales },
                    { nameof(MilestoneDto.DateTime), milestone.DateTime },
                    { nameof(MilestoneDto.DisplayMilestone), milestone.DisplayMilestone },
                });
        }
        
        return Ok(returnListOfMilestones);
    }

    [HttpPost]
    [Route("{milestoneId}/hide")]
    public async Task<IActionResult> HideMilestone([FromRoute] string milestoneId) {
        
        // Get our current UserId and BusinessId to validate and pass to the business service
        string? currentUserId = User.FindFirstValue("Id") ?? null;
        string? currentUserBusinessId = User.FindFirstValue("BusinessId") ?? null;

        // Check there are no issues with the userId
        if (string.IsNullOrEmpty(currentUserId) | string.IsNullOrEmpty(currentUserBusinessId)) {
            return Unauthorized();
        }
        
        // Creating new userDto to pass into service
        UserDto currentUserDto = new UserDto(currentUserId, currentUserBusinessId);

        MilestoneDto milestoneDto =
            await _milestoneService.HideMilestone(currentUserDto, new HideMilestoneDto(milestoneId, currentUserBusinessId));

        if (milestoneDto.ErrorNotification.Errors.ContainsKey("otherErrors")) {
            if (milestoneDto.ErrorNotification.Errors["otherErrors"].Contains(ListOfItemDto.ERROR_UNAUTHORISED)) {
                return Unauthorized();
            }
        }

        return Ok(milestoneDto);
    }
}