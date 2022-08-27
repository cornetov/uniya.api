using System;
using System.Text;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace Uniya.Core;

/// <summary>The type of a connector.</summary>
public enum XConnectorType
{
    /// <summary>Exist ODATA service.</summary>
    OData,
    /// <summary>Microsoft SQL Server.</summary>
    MsSql,
    /// <summary>SQLite server.</summary>
    SQLite,
    /// <summary>Microsoft SharePoint server.</summary>
    SharePoint,
    /// <summary>1C Bitrix 24 server.</summary>
    Bitrix24,
    /// <summary>MS Exchange post server.</summary>
    MsExchange,
}
/*
/// <summary>The connection parameters to a server.</summary>
public partial class XConnection : ITimed
{
    /// <summary>Gets or sets active connection flag.</summary>
    public bool Active { get; set; }
    /// <summary>Gets or sets name of the connection (required).</summary>
    [Required]
    public string Name { get; set; }
    /// <summary>Gets or sets title of the connection (optional).</summary>
    public string Title { get; set; }

    /// <summary>Gets or sets class name of connection.</summary>
    public string ClassName { get; set; }

    /// <summary>Gets or sets URL of the remote host server.</summary>
    [Required]
    public string HostUrl { get; set; }
    ///// <summary>Gets or sets port of the remote host server.</summary>
    //public int HostPort { get; set; }

    /// <summary>Gets or sets user name for authenticate in the remote server.</summary>
    public string HostUserName { get; set; }
    /// <summary>Gets or sets password for authenticate in the remote server.</summary>
    public string HostPassword { get; set; }

    /// <summary>Gets or sets certificate for authenticate with in remote server.</summary>
    public string HostCertificate { get; set; }

    /// <summary>Gets or sets complex code (connection string or same added code).</summary>
    public string ComplexCode { get; set; }
    /// <summary>Gets or sets extended identifier for the client or other secure text.</summary>
    public string SecureText { get; set; }

    /// <summary>Gets or sets URL of the remote server.</summary>
    public string ProxyUri { get; set; }
    /// <summary>Gets or sets user name for authenticate in the remote server.</summary>
    public string ProxyUserName { get; set; }
    /// <summary>Gets or sets password for authenticate in the remote server.</summary>
    public string ProxyPassword { get; set; }

    /// <summary>Gets object identifier.</summary>
    [Key]
    public long Id { get; internal set; }
    /// <summary>Gets or sets UTC date and time of create.</summary>
    [Required]
    public DateTime Created { get; internal set; }
    /// <summary>Gets or sets UTC date and time of last change.</summary>
    [Required]
    public DateTime Modified { get; internal set; }

    /// <summary>Gets or sets connector type of the remote server.</summary>
    [Required]
    public XConnectorType Type { get; set; }
    /// <summary>Gets or sets extension data (optional).</summary>
    public string Tag { get; set; }

    /// <summary>
    /// Returns a string that represents the current object.
    /// </summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
    {
        if (string.IsNullOrEmpty(Title))
        {
            return HostUrl;
        }
        return Title;
    }
}

/// <summary>Contains a collection of connections.</summary>
[XmlRoot("XConnections")]
public sealed class XConnectionCollection : ObservableCollection<XConnection>
{
}
*/
