using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

using Uniya.Core;
using Uniya.Web.Models;

namespace Uniya.Web.Controllers;

/// <summary>Authenticate.</summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthenticateController : ControllerBase
{
    public static IUser user = XProxy.Get<IUser>();
    //private readonly IConfiguration _configuration;
    private readonly IUserService _userService;

    //public AuthController(IConfiguration configuration, IUserService userService)
    public AuthenticateController(IUserService userService)
    {
        //_configuration = configuration;
        _userService = userService;
    }

    [HttpGet, Authorize]
    public ActionResult<UserInfo> GetMe()
    {
        var userInfo = new UserInfo()
        {
            userId = 1,
            userName = _userService.GetMyName(),
            userRoles = _userService.GetMyRoles()
        };
        return Ok(userInfo);
    }

    [HttpPost("register")]
    public async Task<ActionResult<long>> Register(UserLogin request)
    {
        XProxy.TestPassword(request.password, out string hash, out string salt);

        user.Id = 1;
        user.Name = request.userName;
        user.PsswdHash = hash;
        user.PsswdSalt = salt;

        return Ok(user.Id);
    }

    [HttpPost("login")]
    public async Task<ActionResult<UserToken>> Login(UserLogin request)
    {
        if (user.Name != request.userName)
        {
            return BadRequest("User not found.");
        }

        if (!XProxy.VerifyPassword(request.password, user.PsswdHash, user.PsswdSalt))
        {
            return BadRequest("Wrong password.");
        }

        return Ok(_userService.Login(user));
    }

    [HttpPost]
    [Route("refresh")]
    public async Task<IActionResult> Refresh(UserToken userToken)
    {
        if (userToken is null || userToken.refreshToken is null)
        {
            return BadRequest("Invalid refresh request");
        }
        try
        {
            return Ok(_userService.Refresh(userToken));
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    //private string CreateToken(User user)
    //{
    //    List<Claim> claims = new List<Claim>
    //    {
    //        new Claim(ClaimTypes.Name, user.Username),
    //        new Claim(ClaimTypes.Role, "Reader"),
    //        new Claim(ClaimTypes.Role, "Writer"),
    //        new Claim(ClaimTypes.Role, "Admin")
    //    };

    //    var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(
    //        _configuration.GetSection("AppSettings:Secret").Value));

    //    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

    //    var token = new JwtSecurityToken(
    //        claims: claims,
    //        expires: DateTime.Now.AddDays(1),
    //        signingCredentials: creds);

    //    var jwt = new JwtSecurityTokenHandler().WriteToken(token);

    //    return jwt;
    //}
}
