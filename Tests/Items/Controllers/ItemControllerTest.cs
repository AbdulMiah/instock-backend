﻿using FluentAssertions;
using instock_server_application.Businesses.Controllers;
using instock_server_application.Businesses.Models;
using instock_server_application.Businesses.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using static instock_server_application.Tests.Items.MockData.ItemMock;

namespace instock_server_application.Tests.Items.Controllers; 

public class ItemControllerTest {

    [Fact]
    public void Test_GetItem_CorrectItemsForCorrectBusinessId() {
        // Arrange
        const string businessId = "2a36f726-b3a2-11ed-afa1-0242ac120002";
        BusinessIdModel businessIdModel = new BusinessIdModel(businessId);
        List<Item> expected = ItemsList();
        var mockItemService = new Mock<IItemService>();
        var mockBusinessService = new Mock<IBusinessService>();
        mockBusinessService.Setup(service => service.CheckBusinessIdInJWT(null, businessId)).Returns(true);
        mockItemService.Setup(service => service.GetItems(businessId)).Returns(Task.FromResult(expected));
        var controller = new ItemController(mockItemService.Object, mockBusinessService.Object);

        // Act
        var result = controller.GetAllItems(businessIdModel);
        
        // Assert
        Assert.IsAssignableFrom<Task<IActionResult>>(result);
        var okResult = result.Result as OkObjectResult;
        okResult.StatusCode.Should().Be(200);
        okResult.Value.Should().Be(expected);
    }
    
    [Fact]
    public void Test_GetItem_CorrectItemsForIncorrectBusinessId() {
        // Arrange
        const string businessId = "2a36f726-b3a2-11ed-afa1-0242ac120002";
        BusinessIdModel businessIdModel = new BusinessIdModel("test123");
        var mockItemService = new Mock<IItemService>();
        var mockBusinessService = new Mock<IBusinessService>();
        mockBusinessService.Setup(service => service.CheckBusinessIdInJWT(null, "test123")).Returns(true);
        mockItemService.Setup(service => service.GetItems(businessId)).Returns(Task.FromResult(ItemsList()));
        var controller = new ItemController(mockItemService.Object, mockBusinessService.Object);

        // Act
        var result = controller.GetAllItems(businessIdModel);
        
        // Assert
        Assert.IsAssignableFrom<Task<IActionResult>>(result);
        var okResult = result.Result as NotFoundObjectResult;
        okResult.StatusCode.Should().Be(404);
        okResult.Value.Should().Be("No Items Found");
    }

    [Fact]
    public void Test_GetItem_NoAccessForBusinessID() {
        // Arrange
        const string businessId = "2a36f726-b3a2-11ed-afa1-0242ac120002";
        BusinessIdModel businessIdModel = new BusinessIdModel("test123");
        var mockItemService = new Mock<IItemService>();
        var mockBusinessService = new Mock<IBusinessService>();
        mockBusinessService.Setup(service => service.CheckBusinessIdInJWT(null, businessId)).Returns(true);
        mockItemService.Setup(service => service.GetItems(businessId)).Returns(Task.FromResult(ItemsList()));
        var controller = new ItemController(mockItemService.Object, mockBusinessService.Object);

        // Act
        var result = controller.GetAllItems(businessIdModel);
        
        // Assert
        Assert.IsAssignableFrom<Task<IActionResult>>(result);
        var unauthorizedResult = result.Result as UnauthorizedResult;
        unauthorizedResult.StatusCode.Should().Be(401);
    }
}