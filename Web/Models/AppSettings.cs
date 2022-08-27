namespace Uniya.Web.Models;


/// <summary>The application settings.</summary>
public class AppSettings
{
    /// <summary>Gets or sets period of access token live in minutes.</summary>
    public int FastInMinutes { get; set; } = 15;
    /// <summary>Gets or sets period of refresh token live in days.</summary>
    public int LongInDays { get; set; } = 30;

    /// <summary>Gets or sets secret part of a token.</summary>
    public string Secret { get; set; } = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9";
    /// <summary>Gets or sets connection string for main database.</summary>
    public string? ConnectionString { get; set; }
}
