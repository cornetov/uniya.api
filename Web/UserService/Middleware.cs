namespace Uniya.Web.UserService;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
//using Microsoft.IdentityModel.Tokens;

//using System.IdentityModel.Tokens.Jwt;
//using System.Security.Claims;
//using System.Text;

//using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;


using Uniya.Web.Models;


public class JwtMiddleware
{
    private readonly RequestDelegate _next;
    private readonly AppSettings _appSettings;

    public JwtMiddleware(RequestDelegate next, IOptions<AppSettings> appSettings)
    {
        _next = next;
        _appSettings = appSettings.Value;
    }

    public async Task Invoke(HttpContext context, IUserService userService, IJwtService jwtService)
    {
        var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
        //var userId = ValidateJwtToken(token);
        if (jwtService.ValidateAccessToken(token, out string userName, out List<string> userRoles))
        {
            //var user = jwtService.ValidateJwtToken(token);

            // user with roles
            //var userName = UserService.GetMyName(context);
            //var userRoles = UserService.GetMyRoles(context);

            // attach user to context on successful JWT validation
            context.Items["User"] = userName;
            foreach (var role in userRoles)
                context.Items["Role"] = role;
        }

        await _next(context);
    }

    //public string GenerateJwtToken(User user)
    //{
    //    // generate token that is valid for 7 days
    //    var tokenHandler = new JwtSecurityTokenHandler();
    //    var key = Encoding.ASCII.GetBytes(_appSettings.Secret);
    //    var tokenDescriptor = new SecurityTokenDescriptor
    //    {
    //        Subject = new ClaimsIdentity(new[] { new Claim("id", user.Id.ToString()) }),
    //        Expires = DateTime.UtcNow.AddDays(7),
    //        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
    //    };
    //    var token = tokenHandler.CreateToken(tokenDescriptor);
    //    return tokenHandler.WriteToken(token);
    //}


    //public static int? ValidateJwtToken(string token)
    //{
    //    if (token == null)
    //        return null;

    //    var tokenHandler = new JwtSecurityTokenHandler();
    //    var key = Encoding.ASCII.GetBytes(_appSettings.Secret);
    //    try
    //    {
    //        tokenHandler.ValidateToken(token, new TokenValidationParameters
    //        {
    //            ValidateIssuerSigningKey = true,
    //            IssuerSigningKey = new SymmetricSecurityKey(key),
    //            ValidateIssuer = false,
    //            ValidateAudience = false,
    //            // set clock-skew to zero so tokens expire exactly at token expiration time (instead of 5 minutes later)
    //            ClockSkew = TimeSpan.Zero
    //        }, out SecurityToken validatedToken);

    //        var jwtToken = (JwtSecurityToken)validatedToken;
    //        var userId = int.Parse(jwtToken.Claims.First(x => x.Type == "id").Value);

    //        // return user id from JWT token if validation successful
    //        return userId;
    //    }
    //    catch
    //    {
    //        // return null if validation fails
    //        return null;
    //    }
    //}

}

public class ErrorHandlerMiddleware
{
    private readonly RequestDelegate _next;

    public ErrorHandlerMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception error)
        {
            var response = context.Response;
            response.ContentType = "application/json";

            switch (error)
            {
                case AppException e:
                    // custom application error
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    break;
                case KeyNotFoundException e:
                    // not found error
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    break;
                default:
                    // unhandled error
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    break;
            }

            var result = JsonSerializer.Serialize(new { message = error?.Message });
            await response.WriteAsync(result);
        }
    }
}
