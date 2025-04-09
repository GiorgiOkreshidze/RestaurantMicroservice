// using Microsoft.AspNetCore.Authorization;
// using Microsoft.AspNetCore.Identity;
// using Microsoft.AspNetCore.Mvc;
// using Microsoft.IdentityModel.Tokens;
// using System.IdentityModel.Tokens.Jwt;
// using System.Security.Claims;
// using System.Text;
// using Restaurant.Application.DTOs;
// using Restaurant.Application.Interfaces;
//
// namespace Restaurant.API.Controllers
// {
//     [Route("api/[controller]")]
//     [ApiController]
//     public class AuthController : ControllerBase
//     {
//         private readonly UserManager<IdentityUser> _userManager;
//         private readonly SignInManager<IdentityUser> _signInManager;
//         private readonly IConfiguration _configuration;
//
//         public AuthController(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager, IConfiguration configuration)
//         {
//             _userManager = userManager;
//             _signInManager = signInManager;
//             _configuration = configuration;
//         }
//
//         [HttpPost("register")]
//         public async Task<IActionResult> Register([FromBody] RegisterDto model)
//         {
//             if (!ModelState.IsValid)
//                 return BadRequest(ModelState);
//
//             var user = new IdentityUser { UserName = model.Email, Email = model.Email };
//             var result = await _userManager.CreateAsync(user, model.Password);
//
//             if (!result.Succeeded)
//                 return BadRequest(result.Errors);
//
//             return Ok();
//         }
//
//         [HttpPost("login")]
//         public async Task<IActionResult> Login([FromBody] LoginDto model)
//         {
//             if (!ModelState.IsValid)
//                 return BadRequest(ModelState);
//
//             var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, false, false);
//
//             if (!result.Succeeded)
//                 return Unauthorized();
//
//             var user = await _userManager.FindByEmailAsync(model.Email);
//             var token = GenerateJwtToken(user);
//
//             return Ok(new { token });
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
