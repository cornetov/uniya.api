namespace Uniya.Web.Models;

using Uniya.Core;

public class UserContext
{
    public List<IUser> Users { get; set; }

    private readonly AppSettings _settings;

    public UserContext(AppSettings settings)
    {
        _settings = settings;
        Users = new List<IUser>();
    }
}