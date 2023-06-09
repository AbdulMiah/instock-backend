﻿using instock_server_application.Security.Dtos;
using instock_server_application.Security.Services.Interfaces;
using instock_server_application.Users.Models;
using instock_server_application.Users.Repositories.Interfaces;
using instock_server_application.Util.Dto;

namespace instock_server_application.Security.Services; 

public class RefreshTokenService : IRefreshTokenService {
    private readonly IUserRepo _userRepo;
    private static readonly Random Random = new ();

    public RefreshTokenService(IUserRepo userRepo) { _userRepo = userRepo; }

    public string GenerateRandString() {
        // Create String List
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        // Generate Random String of length 500 based on characters
        return new string(
            Enumerable.Repeat(chars, 500)
                .Select(s => s[Random.Next(s.Length)])
                .ToArray()
        );
    }
    
    public string GenerateExpiry() {
        return DateTime.UtcNow.AddDays(90).ToString();
    }
    
    public void CreateToken(RefreshTokenDto refreshTokenDto) {
        // Get User Model from DTO
        User user = refreshTokenDto.User;

        user.RefreshToken = GenerateRandString();
        user.RefreshTokenExpiry = GenerateExpiry();

        // Save User (with new token)
        _userRepo.Save(new UserDto(user));
    }
}