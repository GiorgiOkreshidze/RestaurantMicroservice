// using Microsoft.AspNetCore.Identity;
// using Microsoft.Extensions.Configuration;
// using Microsoft.IdentityModel.Tokens;
// using Restaurant.Application.DTOs;
// using Restaurant.Application.Interfaces;
// using System;
// using System.IdentityModel.Tokens.Jwt;
// using System.Security.Claims;
// using System.Text;
// using System.Threading.Tasks;
//
// namespace Restaurant.Application.Services
// {
//     public class AuthService : IAuthService
//     {
//         private readonly UserManager<IdentityUser> _userManager;
//         private readonly SignInManager<IdentityUser> _signInManager;
//         private readonly IConfiguration _configuration;
//
//         public AuthService(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager, IConfiguration configuration)
//         {
//             _userManager = userManager;
//             _signInManager = signInManager;
//             _configuration = configuration;
//         }
//
//         public async Task<bool> RegisterAsync(RegisterDto model)
//         {
//             var user = new IdentityUser { UserName = model.Email, Email = model.Email };
//             var result = await _userManager.CreateAsync(user, model.Password);
//
//             return result.Succeeded;
//         }
//
//         public async Task<string> LoginAsync(LoginDto model)
//         {
//             var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, false, false);
//
//             if (!result.Succeeded)
//                 return null;
//
//             var user = await _userManager.FindByEmailAsync(model.Email);
//             return GenerateJwtToken(user);
//         }
//
//         private string GenerateJwtToken(IdentityUser user)
//         {
//             var claims = new[]
//             {
//                 new Claim(JwtRegisteredClaimNames.Sub, user.Id),
//                 new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
//                 new Claim(ClaimTypes.NameIdentifier, user.Id)
//             };
//
//             var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
//             var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
//
//             var token = new JwtSecurityToken(
//                 issuer: _configuration["Jwt:Issuer"],
//                 audience: _configuration["Jwt:Audience"],
//                 claims: claims,
//                 expires: DateTime.Now.AddMinutes(double.Parse(_configuration["Jwt:ExpireMinutes"])),
//                 signingCredentials: creds
//             );
//
//             return new JwtSecurityTokenHandler().WriteToken(token);
//         }
//     }
// }
