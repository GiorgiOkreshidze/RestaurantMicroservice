﻿using AutoMapper;
 using FluentValidation;
 using Microsoft.AspNetCore.Identity;
 using Restaurant.Application.DTOs.Auth;
 using Restaurant.Application.DTOs.Users;
using Restaurant.Application.Exceptions;
using Restaurant.Application.Interfaces;
using Restaurant.Domain.Entities;
using Restaurant.Infrastructure.Interfaces;

namespace Restaurant.Application.Services;

public class UserService(
    IUserRepository userRepository, 
    IMapper mapper, 
    IValidator<UpdatePasswordRequest> updatePasswordValidator,
    IPasswordHasher<User> passwordHasher) : IUserService
{
    public async Task<UserDto> GetUserByIdAsync(string id)
    {
        var user = await userRepository.GetUserByIdAsync(id);
        if (user == null)
        {
            throw new NotFoundException("User", id);
        }
        
        return mapper.Map<UserDto>(user);
    }

    public async Task<List<UserDto>> GetAllUsersAsync()
    {
        var users = await userRepository.GetAllUsersAsync();
        return mapper.Map<List<UserDto>>(users);
    }

    public async Task UpdatePasswordAsync(string userId, UpdatePasswordRequest request)
    {
        await ValidateRequestAsync(request, updatePasswordValidator);
        var user = await userRepository.GetUserByIdAsync(userId) ?? throw new NotFoundException("User", userId);
        
        var passwordVerificationResult = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.OldPassword);
        if (passwordVerificationResult == PasswordVerificationResult.Failed)
        {
            throw new BadRequestException("Verification failed.");
        }

        var newPasswordHash = passwordHasher.HashPassword(user, request.NewPassword);
        await userRepository.UpdatePasswordAsync(userId, newPasswordHash);
    }
    
    private static async Task ValidateRequestAsync<T>(T request, IValidator<T> validator)
    {
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            throw new BadRequestException("Invalid Request", validationResult);
        }
    }
}
