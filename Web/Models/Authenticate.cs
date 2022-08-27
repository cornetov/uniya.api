namespace Uniya.Web.Models;


public class UserLogin
{
    public string userName { get; set; } = string.Empty;
    public string password { get; set; } = string.Empty;
}

public class UserToken
{
    public string accessToken { get; set; } = string.Empty;
    public string refreshToken { get; set; } = string.Empty;
}

public class UserInfo
{
    public long userId { get; set; } = 0;
    public string userName { get; set; } = string.Empty;
    public string[]? userRoles { get; set; }
}