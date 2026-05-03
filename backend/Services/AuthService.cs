using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using YtApi.Data;

namespace YtApi.Services;

public class AuthService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public AuthService(AppDbContext db, IConfiguration config) { _db = db; _config = config; }

    public async Task<object?> Login(string email, string password)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash)) return null;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Secret"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var days = int.TryParse(_config["Jwt:ExpirationDays"], out var d) ? d : 7;

        var token = new JwtSecurityToken(
            claims: [new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), new Claim(ClaimTypes.Email, user.Email)],
            expires: DateTime.UtcNow.AddDays(days),
            signingCredentials: creds);

        return new
        {
            accessToken = new JwtSecurityTokenHandler().WriteToken(token),
            user = new { user.Id, user.Email, user.Name }
        };
    }
}
