using Microsoft.AspNetCore.Mvc;
using YtApi.Services;

namespace YtApi.Controllers;

[ApiController, Route("api/auth")]
public class AuthController(AuthService auth) : ControllerBase
{
    public record LoginDto(string Email, string Password);

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var result = await auth.Login(dto.Email, dto.Password);
        return result == null ? Unauthorized(new { message = "Geçersiz giriş bilgileri" }) : Ok(result);
    }
}
