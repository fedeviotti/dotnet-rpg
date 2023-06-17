using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using dotnet_rpg.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace dotnet_rpg.Data;

public class AuthRepository : IAuthRepository
{
    private readonly DataContext _context;
    private readonly IConfiguration _configuration;

    public AuthRepository(DataContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<ServiceResponse<string>> Login(string username, string password)
    {
        var response = new ServiceResponse<string>();
        var user = await _context.Users.FirstOrDefaultAsync((u) =>
            string.Equals(u.Username.ToLower(), username.ToLower(), StringComparison.Ordinal));
        if (user is null)
        {
            response.Success = false;
            response.Message = "User not found.";
        }
        else if (!VerifyPasswordHash(password, user.PasswordHash, user.PasswordSalt))
        {
            response.Success = false;
            response.Message = "Wrong password.";
        }
        else
        {
            response.Data = CreateToken(user);
        }

        return response;
    }

    public async Task<ServiceResponse<int>> Register(User user, string password)
    {
        var response = new ServiceResponse<int>();
        if (await UserExists(user.Username))
        {
            response.Success = false;
            response.Message = "User already exists.";
            return response;
        }
        
        CreatePasswordHash(password, out byte[] passwordHash, out byte[] passwordSalt);
        
        user.PasswordHash = passwordHash;
        user.PasswordSalt = passwordSalt;
        
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        response.Data = user.Id;
        return response;
    }

    public async Task<bool> UserExists(string username)
    {
        return await _context.Users.AnyAsync((u) =>
            string.Equals(u.Username.ToLower(), username.ToLower(), StringComparison.Ordinal));
    }
    
    private static void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
    {
        using var hmac = new HMACSHA512();
        passwordSalt = hmac.Key;
        passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
    }
    
    private static bool VerifyPasswordHash(string password, byte[] passwordHash, byte[] passwordSalt)
    {
        using var hmac = new HMACSHA512(passwordSalt);
        var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
        return computedHash.SequenceEqual(passwordHash);
    }

    private string CreateToken(User user)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
        };
        
        var appSettingsSection = _configuration.GetSection("AppSettings:Token").Value;
        if (appSettingsSection is null)
        {
            throw new Exception("AppSettings Token is null.");
        }
        
        SymmetricSecurityKey key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(appSettingsSection));
        
        SigningCredentials credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddDays(1),
            SigningCredentials = credentials
        };
        
        JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
        SecurityToken token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
        
    }
}