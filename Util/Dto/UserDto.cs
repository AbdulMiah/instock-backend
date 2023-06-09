﻿using instock_server_application.Users.Models;

namespace instock_server_application.Util.Dto; 

public class UserDto {
    public string UserId { get; }
    public string Email { get; }
    public string AccountStatus { get; }
    public long CreationDate { get; }
    public string FirstName { get; }
    public string LastName { get; }
    public string Password { get; }
    public string Role { get; }
    public string UserBusinessId { get; }
    public string RefreshToken { get; }
    public string RefreshTokenExpiry { get; }
    public string? ImageFilename { get; }

    public UserDto(string userId, string email, string accountStatus, long creationDate, string firstName, string lastName, string password, string role, string userBusinessId, string refreshToken, string refreshTokenExpiry, string? imageFilename) {
        UserId = userId;
        Email = email;
        AccountStatus = accountStatus;
        CreationDate = creationDate;
        FirstName = firstName;
        LastName = lastName;
        Password = password;
        Role = role;
        UserBusinessId = userBusinessId;
        RefreshToken = refreshToken;
        RefreshTokenExpiry = refreshTokenExpiry;
        ImageFilename = imageFilename;
    }
    
    public UserDto(string userId, string userBusinessId) {
        UserId = userId;
        UserBusinessId = userBusinessId;
    }

    public UserDto(User user) {
        UserId = user.UserId;
        Email = user.Email;
        AccountStatus = user.AccountStatus;
        CreationDate = user.CreationDate;
        FirstName = user.FirstName;
        LastName = user.LastName;
        Password = user.Password;
        Role = user.Role;
        UserBusinessId = user.BusinessId;
        RefreshToken = user.RefreshToken;
        RefreshTokenExpiry = user.RefreshTokenExpiry;
        ImageFilename = user.ImageFilename;
    }
}