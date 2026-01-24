using GisProject.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GisProject.Interfaces;
using GisProject.Services;

namespace GisProject.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginModel model)
        {
            var token = _authService.Login(model.Username, model.Password);

            if (token == null)
            {
                return Unauthorized("帳號或密碼錯誤");
            }

            return Ok(new { token = token });
        }
    }
}
