using GisProject.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace GisProject.Controllers
{
    [Route("api/[controller]")] // 這代表路徑是 /api/Auth
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public AuthController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginModel model)
        {
            // 簡單測試用邏輯 (實務上會比對資料庫)
            if (model.Username == "admin" && model.Password == "password123")
            {
                var token = GenerateJwtToken(model.Username);
                return Ok(new { token = token }); // 務必回傳一個物件
            }
            return Unauthorized("帳號或密碼錯誤");
        }

        private string GenerateJwtToken(string username)
        {
            // 1. 取得你在 Program.cs 設定的同一把鑰匙
            var jwtKey = _configuration["Jwt:Key"] ?? "GeoNexus_Super_Secret_Key_2026_Keep_It_Safe";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // 2. 設定通行證內容 (Claims)
            var claims = new[]
            {
                    new Claim(JwtRegisteredClaimNames.Sub, username),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim(ClaimTypes.Name, username)
            };

            // 3. 製作 Token
            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"] ?? "GeoNexus-App",
                audience: _configuration["Jwt:Audience"] ?? "GeoNexus-Frontend",
                claims: claims,
                expires: DateTime.Now.AddHours(3), // 通行證有效期限 3 小時
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
