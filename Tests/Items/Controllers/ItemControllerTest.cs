using System.Security;
using System.Security.Claims;
using FluentAssertions;
using instock_server_application.AwsS3.Dtos;
using instock_server_application.AwsS3.Services.Interfaces;
using instock_server_application.Businesses.Controllers;
using instock_server_application.Businesses.Controllers.forms;
using instock_server_application.Businesses.Dtos;
using instock_server_application.Businesses.Repositories.Interfaces;
using instock_server_application.Businesses.Services;
using instock_server_application.Businesses.Services.Interfaces;
using instock_server_application.Shared.Dto;
using instock_server_application.Util.Dto;
using instock_server_application.Util.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using static instock_server_application.Tests.Items.MockData.ItemMock;

namespace instock_server_application.Tests.Items.Controllers; 

public class ItemControllerTest {

    [Fact]
    public async Task Test_GetItem_CorrectItemsForCorrectBusinessId() {
        // Arrange
        const string businessId = "2a36f726-b3a2-11ed-afa1-0242ac120002";
        const string userId = "UID123";
        
        var mockUser = new ClaimsPrincipal(
            new ClaimsIdentity(
                new List<Claim>() {
                    new Claim("Id", userId),
                    new Claim("BusinessId", businessId)
                }, "mockUserAuth"));
        
        ListOfItemDto itemServiceResponse = new ListOfItemDto(new List<ItemDto>() {
            new ItemDto("sku", "business_id", "category", "name", 10, 5, 5, "")
        });
        
        var mockItemService = new Mock<IItemService>();
        mockItemService.Setup(service => service.GetItems(It.IsAny<UserDto>(), businessId)).Returns(Task.FromResult(itemServiceResponse)!);
        var mockIStorageService = new Mock<IStorageService>();
        mockIStorageService.Setup(service => service.GetFilePresignedUrl(It.IsAny<string>(),It.IsAny<string>())).Returns(new S3ResponseDto());
        
        var controller = new ItemController(mockItemService.Object, mockIStorageService.Object);
        controller.ControllerContext = new ControllerContext() {
             HttpContext = new DefaultHttpContext() { User = mockUser }
         };
        
        // Act
        var result = await controller.GetAllItems(businessId);
        
        // Assert
        Assert.IsAssignableFrom<IActionResult>(result);
        var okResult = result as OkObjectResult;
        
        okResult.StatusCode.Should().Be(200);

        var expected = new List<Dictionary<string, object>> {
            new() {
                { "SKU", "sku" },
                { "BusinessId", "business_id" },
                { "Category", "category" },
                { "Name", "name" },
                { "Stock", 10 },
                { "TotalStock", 10 },
                { "TotalOrders", 5 },
                { "AvailableStock", 5 },
                { "ImageUrl", "" }
            }
        };
        Assert.Equal(expected.ToString(), okResult.Value.ToString());
    }
    
    [Fact]
    public void Test_GetItem_CorrectItemsForIncorrectBusinessId() {
        // Arrange
        String incorrectBusinessId = "test123";
        const string userId = "UID123";

        ListOfItemDto expected = new ListOfItemDto(new List<ItemDto>());
        var mockUser = new ClaimsPrincipal(
            new ClaimsIdentity(
                new List<Claim>() {
                    new Claim("Id", userId),
                    new Claim("BusinessId", incorrectBusinessId)
                }, "mockUserAuth"));
        var mockItemService = new Mock<IItemService>();
        mockItemService.Setup(service => service.GetItems(It.IsAny<UserDto>(), incorrectBusinessId)).Returns(Task.FromResult(expected)!);
        var mockIStorageService = new Mock<IStorageService>();
        mockIStorageService.Setup(service => service.GetFilePresignedUrl(It.IsAny<string>(),It.IsAny<string>())).Returns(new S3ResponseDto());
        
        var controller = new ItemController(mockItemService.Object, mockIStorageService.Object);
        controller.ControllerContext = new ControllerContext() {
            HttpContext = new DefaultHttpContext() { User = mockUser }
        };
        
        // Act
        var result = controller.GetAllItems(incorrectBusinessId);
        
        // Assert
        Assert.IsAssignableFrom<Task<IActionResult>>(result);
        var notFoundResult = result.Result as NotFoundResult;
        notFoundResult.StatusCode.Should().Be(404);
    }

    [Fact]
    public void Test_GetItem_NoAccessForBusinessID() {
        // Arrange
        const string businessId = "2a36f726-b3a2-11ed-afa1-0242ac120002";
        const string userId = "UID123";

        var mockUser = new ClaimsPrincipal(
            new ClaimsIdentity(
                new List<Claim>() {
                    new Claim("Id", userId),
                    new Claim("BusinessId", businessId)
                }, "mockUserAuth"));
        
        var mockItemService = new Mock<IItemService>();
        ListOfItemDto mockReturn = new ListOfItemDto(new List<ItemDto>());
        mockItemService.Setup(service => service.GetItems(It.IsAny<UserDto>(), businessId)).Returns(Task.FromResult(mockReturn));
        var mockIStorageService = new Mock<IStorageService>();
        mockIStorageService.Setup(service => service.GetFilePresignedUrl(It.IsAny<string>(),It.IsAny<string>())).Returns(new S3ResponseDto());
        
        var controller = new ItemController(mockItemService.Object, mockIStorageService.Object);
        controller.ControllerContext = new ControllerContext() {
            HttpContext = new DefaultHttpContext() { User = mockUser }
        };

        // Act
        var result = controller.GetAllItems(businessId);
        
        // Assert
        Assert.IsAssignableFrom<Task<IActionResult>>(result);
        var unauthorizedResult = result.Result as UnauthorizedResult;
        unauthorizedResult?.StatusCode.Should().Be(401);
    }
    
    [Fact]
    public async Task Test_CreateItem_WithCorrectFormDetails() {
        // Arrange
        const string businessId = "2a36f726-b3a2-11ed-afa1-0242ac120002";
        const string userId = "UID123";
        
        var createItemForm = new CreateItemForm("Test-SKU-123",
            "Test Category",
            "Test Item Name",
            10,
            null
        );
        
        var mockUser = new ClaimsPrincipal(
            new ClaimsIdentity(
                new List<Claim>() {
                    new Claim("Id", userId),
                    new Claim("BusinessId", businessId)
                }, "mockUserAuth"));
        var expected = new ItemDto(
            "Test-SKU-123",
            businessId,
            "Test Category",
            "Test Item Name",
            10, 
            0,
            0,
            "https://image.png");

        // Mock url for url helper to return
        string returnedUrl = "http://returned/";
        
        var mockItemService = new Mock<IItemService>();
        mockItemService.Setup(service => service.CreateItem(It.IsAny<CreateItemRequestDto>())).Returns(Task.FromResult(expected)!);
        var mockIStorageService = new Mock<IStorageService>();
        mockIStorageService.Setup(service => service.GetFilePresignedUrl(It.IsAny<string>(),It.IsAny<string>())).Returns(new S3ResponseDto());
        
        var controller = new ItemController(mockItemService.Object, mockIStorageService.Object);
        controller.ControllerContext = new ControllerContext() {
            HttpContext = new DefaultHttpContext() { User = mockUser }
        };
        
        // Mock url helper for controller
        var mockUrlHelper = new Mock<IUrlHelper>();
        mockUrlHelper.Setup(x => x.Link(It.IsAny<string>(), It.IsAny<object>())).Returns(returnedUrl);
        controller.Url = mockUrlHelper.Object;

        // Act
        IActionResult response = await controller.CreateItem(createItemForm, businessId);
        
        // Assert
        Assert.IsType<CreatedResult>(response);
    }

    [Fact]
    public void Test_DeleteItem_WithCorrectDetails() {
        // Arrange
        string testBusinessId = "TestBusinessId";
        string testItemId = "TestItemId";
        var mockItemRepo = new Mock<IItemRepo>();
        var mockClaimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] {
            new Claim("BusinessId", "TestBusinessId")
        }));
        var utilService = new UtilService();
        var mockStorageRepo = new Mock<IStorageService>();
        var mockNotifService = new Mock<NotificationService>();
        var mockConnectionsService = new Mock<IConnectionsService>();
        var mockItemConnectionsService = new Mock<IItemConnectionService>();
        var itemService = new ItemService(mockItemRepo.Object, utilService, mockStorageRepo.Object, mockNotifService.Object, mockConnectionsService.Object, mockItemConnectionsService.Object);
        var mockIStorageService = new Mock<IStorageService>();
        mockIStorageService.Setup(service => service.GetFilePresignedUrl(It.IsAny<string>(),It.IsAny<string>())).Returns(new S3ResponseDto());
        
        var itemController = new ItemController(itemService, mockIStorageService.Object) {
            ControllerContext = new ControllerContext {
                HttpContext = new DefaultHttpContext { User = mockClaimsPrincipal }
            }
        };

        // Act
        var response = itemController.DeleteItem(testItemId, testBusinessId);

        // Assert
        Assert.IsAssignableFrom<Task<IActionResult>>(response);
        var result = response.Result as OkResult;
        result.StatusCode.Should().Be(200);
    }

    [Fact]
    public void Test_DeleteItem_IncorrectBusinessId() {
        // Arrange
        string testBusinessId = "IncorrectId";
        string testItemId = "IncorrectId";
        var mockItemRepo = new Mock<IItemRepo>();
        var mockStorageRepo = new Mock<IStorageService>();
        var mockClaimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] { new("BusinessId", "TestBusinessId") }));
        var mockNotifService = new Mock<NotificationService>();
        var mockConnectionsService = new Mock<IConnectionsService>();
        var mockItemConnectionsService = new Mock<IItemConnectionService>();
        var itemService = new ItemService(mockItemRepo.Object, new UtilService(), mockStorageRepo.Object,
            mockNotifService.Object, mockConnectionsService.Object, mockItemConnectionsService.Object);
        var mockIStorageService = new Mock<IStorageService>();
        mockIStorageService.Setup(service => service.GetFilePresignedUrl(It.IsAny<string>(),It.IsAny<string>())).Returns(new S3ResponseDto());

        var itemController = new ItemController(itemService, mockIStorageService.Object) {
            ControllerContext = new ControllerContext {
                HttpContext = new DefaultHttpContext { User = mockClaimsPrincipal }
            }
        };
        // Act
        var response = itemController.DeleteItem(testItemId, testBusinessId);

        // Assert
        Assert.IsAssignableFrom<Task<IActionResult>>(response);
        var result = response.Result as UnauthorizedResult;
        result.StatusCode.Should().Be(401);
    }
    
    [Fact]
    public async Task Test_GetCategories_CorrectCategoriesForCorrectBusinessId() {
        // Arrange
        const string businessId = "2a36f726-b3a2-11ed-afa1-0242ac120002";
        const string userId = "UID123";
        
        var mockUser = new ClaimsPrincipal(
            new ClaimsIdentity(
                new List<Claim>() {
                    new Claim("Id", userId),
                    new Claim("BusinessId", businessId)
                }, "mockUserAuth"));
        List<Dictionary<string, string>> expected = CategoriesList();
        
        var mockItemService = new Mock<IItemService>();
        mockItemService.Setup(service => service.GetCategories(It.IsAny<ValidateBusinessIdDto>())).Returns(Task.FromResult(expected)!);
        var mockIStorageService = new Mock<IStorageService>();
        mockIStorageService.Setup(service => service.GetFilePresignedUrl(It.IsAny<string>(),It.IsAny<string>())).Returns(new S3ResponseDto());
        
        var controller = new ItemController(mockItemService.Object, mockIStorageService.Object);
        controller.ControllerContext = new ControllerContext() {
             HttpContext = new DefaultHttpContext() { User = mockUser }
         };
        
        // Act
        var result = await controller.GetAllCategories(businessId);
        
        // Assert
        Assert.IsAssignableFrom<IActionResult>(result);
        var okResult = result as OkObjectResult;
        
        okResult.StatusCode.Should().Be(200);
        okResult.Value.Should().Be(expected);
    }
    
    [Fact]
    public void Test_GetCategories_CorrectCategoriesForIncorrectBusinessId() {
        // Arrange
        String incorrectBusinessId = "test123";
        const string userId = "UID123";

        List<Dictionary<string, string>> expected = EmptyList();
        var mockUser = new ClaimsPrincipal(
            new ClaimsIdentity(
                new List<Claim>() {
                    new Claim("Id", userId),
                    new Claim("BusinessId", incorrectBusinessId)
                }, "mockUserAuth"));
        var mockItemService = new Mock<IItemService>();
        mockItemService.Setup(service => service.GetCategories(It.IsAny<ValidateBusinessIdDto>())).Returns(Task.FromResult(expected)!);
        var mockIStorageService = new Mock<IStorageService>();
        mockIStorageService.Setup(service => service.GetFilePresignedUrl(It.IsAny<string>(),It.IsAny<string>())).Returns(new S3ResponseDto());
        
        var controller = new ItemController(mockItemService.Object, mockIStorageService.Object);
        controller.ControllerContext = new ControllerContext() {
            HttpContext = new DefaultHttpContext() { User = mockUser }
        };
        
        // Act
        var result = controller.GetAllCategories(incorrectBusinessId);
        
        // Assert
        Assert.IsAssignableFrom<Task<IActionResult>>(result);
        var notFoundResult = result.Result as NotFoundResult;
        notFoundResult.StatusCode.Should().Be(404);
    }

    [Fact]
    public void Test_GetCategories_NoAccessForBusinessID() {
        // Arrange
        const string businessId = "2a36f726-b3a2-11ed-afa1-0242ac120002";
        const string userId = "UID123";

        var mockUser = new ClaimsPrincipal(
            new ClaimsIdentity(
                new List<Claim>() {
                    new Claim("Id", userId),
                    new Claim("BusinessId", businessId)
                }, "mockUserAuth"));
        
        var mockItemService = new Mock<IItemService>();
        mockItemService.Setup(service => service.GetCategories(It.IsAny<ValidateBusinessIdDto>())).Returns(Task.FromResult(ItemsList())!);
        var mockIStorageService = new Mock<IStorageService>();
        mockIStorageService.Setup(service => service.GetFilePresignedUrl(It.IsAny<string>(),It.IsAny<string>())).Returns(new S3ResponseDto());
        
        var controller = new ItemController(mockItemService.Object, mockIStorageService.Object);
        controller.ControllerContext = new ControllerContext() {
            HttpContext = new DefaultHttpContext() { User = mockUser }
        };

        // Act
        var result = controller.GetAllCategories(businessId);
        
        // Assert
        Assert.IsAssignableFrom<Task<IActionResult>>(result);
        var unauthorizedResult = result.Result as UnauthorizedResult;
        unauthorizedResult?.StatusCode.Should().Be(401);
    }
}