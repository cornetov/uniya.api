namespace Uniya.Web.UserService;

using Microsoft.Extensions.Options;

using System.Security.Claims;

using Uniya.Core;
using Uniya.Web.Models;

public interface IUserService
{
    string GetMyName();
    string[] GetMyRoles();
    UserToken Login(IUser user);
    UserToken Refresh(UserToken userToken);
}

public class UserService : IUserService
{
    private HttpContext? _httpContext;
    private UserContext _userContext;
    private IJwtService _jwtService;
    private readonly AppSettings _appSettings;

    public UserService(IHttpContextAccessor context, IJwtService jwt, IOptions<AppSettings> options)
    {
        _httpContext = context.HttpContext;
        _jwtService = jwt;
        _appSettings = options.Value;
        _userContext = new UserContext(_appSettings);
    }

    //private readonly IHttpContextAccessor _httpContextAccessor;

    //public UserService(IHttpContextAccessor httpContextAccessor)
    //{
    //    _httpContextAccessor = httpContextAccessor;
    //}

    //public string Login(string login, string password)
    public UserToken Login(IUser user)
    {
        var userToken = _jwtService.GenerateToken(user);
        // create session
        return userToken;
    }
    public UserToken Refresh(UserToken userToken)
    {
        // update session
        return new UserToken() { accessToken = "", refreshToken = "" };
    }

    public string GetMyName()
    {
        return GetMyName(_httpContext);
        //return GetMyName(_httpContextAccessor.HttpContext);
    }
    public string[] GetMyRoles()
    {
        return GetMyRoles(_httpContext);
    }

    static string GetMyName(HttpContext? httpContext)
    {
        var result = string.Empty;
        if (httpContext != null && httpContext.User != null)
        {
            result = httpContext.User.FindFirstValue(ClaimTypes.Name);
        }
        return result;
    }
    static string[] GetMyRoles(HttpContext? httpContext)
    {
        var result = new List<string>();
        if (httpContext != null && httpContext.User != null)
        {
            foreach (var role in httpContext.User.FindAll(ClaimTypes.Role))
                result.Add(role.Value);
        }
        return result.ToArray();
    }

}
