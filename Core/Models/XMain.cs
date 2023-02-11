using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

#if ID_GUID
using _Id = System.Guid;
#else
using _Id = System.Int64;
#endif

namespace Uniya.Core;

// ----------------------------------------------------------------------------------------
#region ** role support

/// <summary>
/// Defined model roles.
/// </summary>
public static class XRole
{
    /// <summary>Reader</summary>
    public const string Reader = "Reader";
    /// <summary>Writer</summary>
    public const string Writer = "Writer";
    /// <summary>Administrator</summary>
    public const string Administrator = "Admin";
}

/// <summary>Roles.</summary>
public interface IRole : IActiveDB, ITitleDB, INoteDB
{
    /// <summary>Gets or sets parent role.</summary>
    [ForeignKey("Role")]
    [Display(Name = "Role", Description = "Select parent role")]
    _Id ParentId { get; set; }
}

/// <summary>Solutions.</summary>

public interface ISolution : ITitleDB
{
    /// <summary>Gets or sets role identifier.</summary>
    [Required]
    [ForeignKey("Role")]
    [Display(Name = "Role", Description = "Solution's role")]
    _Id RoleId { get; set; }

    /// <summary>Gets or sets parent solution identifier.</summary>
    [Required]
    [ForeignKey("Solution")]
    [Display(Name = "Solution", Description = "Parent solution")]
    _Id ParentId { get; set; }
}

#endregion

// ----------------------------------------------------------------------------------------
#region ** addresses support

/// <summary>
/// Address type.
/// </summary>
public enum XAddressType
{
    /// <summary>Official country (nation) name.</summary>
    [Display(Name = "Country name")]
    Country,
    /// <summary>Official region/subregion name or number.</summary>
    [Display(Name = "Region or area name")]
    RegionOrArea,
    /// <summary>Settlement name or number (city, town, village, etc.)</summary>
    [Display(Name = "Settlement name")]
    Settlement,
    /// <summary>Settlement street/block name or number (avenue, borough, district, alley, etc.).</summary>
    [Display(Name = "Street or block name")]
    StreetOrBlock,
    /// <summary>Building name or number (house, corpus, section, etc.).</summary>
    [Display(Name = "Number or name of a building")]
    Building,
    /// <summary>Apartment name or number (flat, room, studio, etc.).</summary>
    [Display(Name = "Number or name of an apartment")]
    Apartment
}

/// <summary>Address.</summary>
public interface IAddress : IDB
{
    // Gets or sets address type.
    [Required]
    [Display(Name = "Address type", Description = "Select address type")]
    XAddressType Type { get; set; }
    // Gets or sets name or number of this address.
    [Required]
    [Display(Name = "Name or number", Description = "Name or number of this address")]
    string Name { get; set; }

    // Gets or sets index (zip code) or prefix of this address.
    [Display(Name = "Index or ZIP code", Description = "Index or ZIP code of this address")]
    [DataType(DataType.PostalCode)]
    string Index { get; set; }
    // Gets or sets phone number or prefix of this address.
    [Display(Name = "Phone", Description = "Stationary phone number of this address")]
    [DataType(DataType.PhoneNumber)]
    string Phone { get; set; }

    // Gets or sets description of this address.
    [Display(Name = "Description", Description = "About of this address")]
    string Description { get; set; }

    /// <summary>Gets or sets parent address.</summary>
    [ForeignKey("Address")]
    [Display(Name = "Parent address", Description = "Select parent address")]
    _Id ParentId { get; set; }
}

/// <summary>Persons.</summary>
public interface IPerson : IDB
{
    // Gets or sets first name of this person.
    [Required]
    [Display(Name = "First name", Description = "First name of this person")]
    string FirstName { get; set; }
    // Gets or sets second name of this person.
    [Display(Name = "Second name", Description = "Second name of this person")]
    string SecondName { get; set; }
    // Gets or sets last name of this person.
    [Required]
    [Display(Name = "Last name", Description = "Last name (family) of this person")]
    string LastName { get; set; }

    // Gets or sets birthday of this person.
    [Display(Name = "Birthday", Description = "Birthday of this person")]
    [DataType(DataType.Date)]
    DateTime Birthday { get; set; }

    // Gets or sets primary mobile phone number of this person.
    [Display(Name = "Mobile phone", Description = "Mobile phone number of this person")]
    [DataType(DataType.PhoneNumber)]
    string MobilePhone { get; set; }
    // Gets or sets primary alternative phone number of this person.
    [Display(Name = "Alternative phone", Description = "Alternative phone number of this person")]
    [DataType(DataType.PhoneNumber)]
    string AlternativePhone { get; set; }

    // Gets or sets primary personal email address of this person.
    [Display(Name = "Personal e-mail", Description = "Personal e-mail address of this person")]
    [DataType(DataType.EmailAddress)]
    string PersonalEmail { get; set; }
    // Gets or sets alternative personal email address of this person.
    [Display(Name = "Alternative e-mail", Description = "Alternative e-mail address of this person")]
    [DataType(DataType.EmailAddress)]
    string AlternativeEmail { get; set; }

    /// <summary>Gets or sets official address identifier of this person.</summary>
    [ForeignKey("Address")]
    [Display(Name = "Official address", Description = "Select official address")]
    long OfficialAddressId { get; set; }
    /// <summary>Gets or sets real address identifier of this person.</summary>
    [ForeignKey("Address")]
    [Display(Name = "Real address", Description = "Select real address")]
    _Id RealAddressId { get; set; }
    /// <summary>Gets or sets alternative address identifier of this person.</summary>
    [ForeignKey("Address")]
    [Display(Name = "Alternative address", Description = "Select alternative address")]
    _Id AlternativeAddressId { get; set; }
}

#endregion

// ----------------------------------------------------------------------------------------
#region ** user and session support

/// <summary>Users.</summary>
public interface IUser : IActiveDB
{
    /// <summary>Gets or sets name.</summary>
    [Unique]
    [Display(Name = "Login", Description = "Unique login (name or moniker)")]
    string Name { get; set; }
    /// <summary>Gets or sets unique code.</summary>
    [Unique]
    string Code { get; set; }
    /// <summary>Gets or sets user's password hash.</summary>
    [Required]
    [Display(Name = "Password", Description = "The password (8 symbols or more) numbers, upper and lower literalemail address")]
    [DataType(DataType.Password)]
    string PsswdHash { get; set; }
    /// <summary>Gets or sets user's password salt hash.</summary>
    [Required]
    string PsswdSalt { get; set; }

    ///// <summary>
    ///// every time the user changes his Password,
    ///// or an administrator changes his Roles or stat/IsActive,
    ///// create a new `SerialNumber` GUID and store it in the DB.
    ///// </summary>
    //string SerialNumber { get; set; }

    //public string FirstName { get; set; }
    //public string LastName { get; set; }
    //public string UserName { get; set; }
    //public string Password { get; set; }
    //string Role { get; set; }

    // Gets or sets primary phone number of this person.
    [Unique]
    [Display(Name = "Phone", Description = "Mobile phone number")]
    [DataType(DataType.PhoneNumber)]
    string Phone { get; set; }
    // Gets or sets checked or no primary phone number of this person.
    [Display(Name = "Phone checked", Description = "The mobile phone is checked")]
    bool IsPhoneChecked { get; set; }

    // Gets or sets primary email address of this person.
    [Unique]
    [Display(Name = "Email", Description = "Personable email address")]
    [DataType(DataType.EmailAddress)]
    string Email { get; set; }
    // Gets or sets checked or no primary email address of this person.
    [Display(Name = "Email checked", Description = "The user's e-mail is checked")]
    bool IsEmailChecked { get; set; }

    ///// <summary>Gets or sets user's person identifier.</summary>
    [ForeignKey("Person")]
    [Display(Name = "Person", Description = "User's person")]
    _Id PersonId { get; set; }
}

/// <summary>User's session.</summary>
public interface IUserSession : IActiveDB
{
    /// <summary>Gets or sets user identifier.</summary>
    [Required]
    [ForeignKey("User")]
    [Display(Name = "User", Description = "User of this session")]
    _Id UserId { get; set; }
    /// <summary>Gets or sets session refresh GUID.</summary>
    [Required]
    [Display(Name = "Refresh GUID", Description = "Refresh GUID for this session")]
    string RefreshGuid { get; set; }
    /// <summary>Gets or sets expires in date and time.</summary>
    [Required]
    [Display(Name = "Expires In", Description = "Expires in UTC time")]
    DateTime ExpiresIn { get; set; }
    /// <summary>Gets or sets session expires in UTC time.</summary>
    [Required]
    DateTimeOffset LoggedIn { get; set; }
    [Display(Name = "Fingerprint", Description = "Fingerprint data")]
    /// <summary>Gets or sets browser or device fingerprint.</summary>
    string Fingerprint { get; set; }
}

/// <summary>User's roles.</summary>
public interface IUserRole : IActiveDB, INoteDB
{
    /// <summary>Gets or sets user identifier.</summary>
    [Unique("UR")]
    [ForeignKey("User")]
    [Display(Name = "User", Description = "User of the role")]
    _Id UserId { get; set; }
    /// <summary>Gets or sets role identifier.</summary>
    [Unique("UR")]
    [ForeignKey("Role")]
    [Display(Name = "Role", Description = "Role of the user")]
    _Id RoleId { get; set; }
}

/// <summary>User's roles.</summary>
public interface IGroup : IActiveDB, ITitleDB, INoteDB
{
    /// <summary>Gets or sets role identifier.</summary>
    [ForeignKey("Role")]
    [Display(Name = "Role", Description = "Role of the group")]
    _Id RoleId { get; set; }
}

/// <summary>User's roles.</summary>
public interface IGroupUser : IActiveDB, INoteDB
{
    /// <summary>Gets or sets group identifier.</summary>
    [Unique("GU")]
    [ForeignKey("Group")]
    [Display(Name = "Group", Description = "Group of the user")]
    _Id GroupId { get; set; }
    /// <summary>Gets or sets user identifier.</summary>
    [Unique("GU")]
    [ForeignKey("User")]
    [Display(Name = "User", Description = "User of the group")]
    _Id UserId { get; set; }
}

#endregion

// ----------------------------------------------------------------------------------------
#region ** connections and tasks support

/// <summary>The connection parameters to a server.</summary>
public interface IConnection : ITitleDB, IRunDB, INoteDB
{
    /// <summary>Gets or sets URL of the remote host server.</summary>
    [Required]
    [Display(Name = "Host URL", Description = "URL of the remote host server")]
    string HostUrl { get; set; }
    ///// <summary>Gets or sets port of the remote host server.</summary>
    //int HostPort { get; set; }

    /// <summary>Gets or sets user name for authenticate in the remote server.</summary>
    [Display(Name = "Login", Description = "User name (login) of the remote host server")]
    string HostUserName { get; set; }
    /// <summary>Gets or sets password for authenticate in the remote server.</summary>
    [Display(Name = "Password", Description = "Password of the remote host server")]
    [DataType(DataType.Password)]
    string HostPassword { get; set; }

    /// <summary>Gets or sets certificate for authenticate with in remote server.</summary>
    [Display(Name = "Certificate", Description = "Certificate of the remote host server")]
    string HostCertificate { get; set; }
    /// <summary>Gets or sets complex code (connection string or same added code).</summary>
    [Display(Name = "Complex code", Description = "Certificate of the remote host server")]
    [DataType(DataType.Password)]
    string ComplexCode { get; set; }

    /// <summary>Gets or sets extended identifier for the client or other secure text.</summary>
    [Display(Name = "Secure text", Description = "Secure text of the remote host server")]
    [DataType(DataType.Password)]
    string SecureText { get; set; }

    /// <summary>Gets or sets URL of the proxy server.</summary>
    [Display(Name = "Proxy URL", Description = "URL of the proxy server")]
    string ProxyUri { get; set; }
    /// <summary>Gets or sets user name for authenticate in the proxy server.</summary>
    [Display(Name = "Login", Description = "User name (login) of the proxy server")]
    string ProxyUserName { get; set; }
    /// <summary>Gets or sets password for authenticate in the proxy server.</summary>
    [Display(Name = "Password", Description = "Password of the remote host server")]
    [DataType(DataType.Password)]
    string ProxyPassword { get; set; }

    /// <summary>Gets or sets connector type of the remote server.</summary>
    [Display(Name = "Connector type", Description = "Connector type of the remote host server")]
    XConnectorType ConnectorType { get; set; }
    /// <summary>Gets or sets extension data (optional).</summary>
    [Display(Name = "Tag", Description = "Tag of information for the remote host server")]
    string ConnectorTag { get; set; }
}

/// <summary>The task parameters.</summary>
public interface ITask : ITitleDB, IRunDB, INoteDB
{
    /// <summary>Gets or sets autostart task flag.</summary>
    bool Autostart { get; set; }

    /// <summary>Gets or sets start period in minutes.</summary>
    int StartPeriod { get; set; }

    /// <summary>Gets or sets list of the connections by names separated using '|' symbol.</summary>
    string Connections { get; set; }
}

#endregion

// ----------------------------------------------------------------------------------------
#region ** various parameters

public interface IParameter : ITitleDB, INoteDB
{
    /// <summary>Gets or sets value of the parameter.</summary>
    [Required]
    string Value { get; set; }
}

#endregion

// ----------------------------------------------------------------------------------------
#region ** run code support

#endregion

// ----------------------------------------------------------------------------------------
#region ** files in folders support

/// <summary>The folder.</summary>
public interface IFolder : IActiveDB, INoteDB
{
    /// <summary>Gets or sets folder name.</summary>
    [Unique("FP")]
    string Name { get; set; }

    /// <summary>Gets or sets folder unique code.</summary>
    [Required]
    [Unique]
    string UniqueCode { get; set; }

    /// <summary>Gets or sets parent address.</summary>
    [Unique("FP")]
    [ForeignKey("Folder")]
    long ParentId { get; set; }

    /// <summary>Gets or sets access role identifier.</summary>
    [Required]
    [ForeignKey("Role")]
    long AccessRoleId { get; set; }
}
/// <summary>The file.</summary>
public interface IFile : IActiveDB, INoteDB
{
    /// <summary>Gets or sets file name.</summary>
    [Unique("FF")]
    string Name { get; set; }
    /// <summary>Gets or sets file extension.</summary>
    [Required(AllowEmptyStrings = true)]
    string Extension { get; set; }

    /// <summary>Gets or sets file code.</summary>
    [Unique]
    string UniqueCode { get; set; }

    /// <summary>Gets or sets file data.</summary>
    byte[] Data { get; set; }

    /// <summary>Gets or sets access role identifier.</summary>
    [Unique("FF")]
    [ForeignKey("Folder")]
    _Id FolderId { get; set; }

    /// <summary>Gets or sets access role identifier.</summary>
    [Required]
    [ForeignKey("Role")]
    _Id AccessRoleId { get; set; }
}

#endregion

// ----------------------------------------------------------------------------------------
#region ** companies and employees support

/// <summary>The company.</summary>
public interface ICompany : IActiveDB, ITitleDB, INoteDB
{
    /// <summary>Gets or sets official code.</summary>
    string OfficialCode { get; set; }

    // Gets or sets birthday of this person.
    DateTime Birthday { get; set; }

    // Gets or sets primary mobile phone number of this person.
    string OfficialPhone { get; set; }
    // Gets or sets primary alternative phone number of this person.
    string AlternativePhone { get; set; }

    // Gets or sets primary personal email address of this person.
    string OfficialEmail { get; set; }
    // Gets or sets alternative personal email address of this person.
    string AlternativeEmail { get; set; }

    /// <summary>Gets or sets official address identifier of this company.</summary>
    [ForeignKey("Address")]
    _Id OfficialAddressId { get; set; }
    /// <summary>Gets or sets real address identifier of this company.</summary>
    [ForeignKey("Address")]
    _Id RealAddressId { get; set; }
}

/// <summary>The employee of the company.</summary>
public interface IEmployee : IActiveDB, ITitleDB, INoteDB
{
    /// <summary>Gets or sets official code.</summary>
    string OfficialCode { get; set; }

    /// <summary>Gets or sets role identifier.</summary>
    [Required]
    [ForeignKey("Company")]
    _Id CompanyId { get; set; }

    /// <summary>Gets or sets user's person identifier.</summary>
    [Required]
    [ForeignKey("Person")]
    _Id PersonId { get; set; }
}

#endregion

// ----------------------------------------------------------------------------------------
#region ** topics by categories support

/// <summary>
/// The topic type.
/// </summary>
public enum XTopicType
{
    /// <summary>News</summary>
    News,
    /// <summary>Information</summary>
    Info,
    /// <summary>Review</summary>
    //Review,
    /// <summary>Offer</summary>
    //Offer,
}

/// <summary>The topic of the company.</summary>
public interface ITopic : IActiveDB, ITitleDB, INoteDB
{
    /// <summary>Gets or sets topic type.</summary>
    [Required]
    XTopicType Type { get; set; }

    /// <summary>Gets or sets topic context.</summary>
    [Required]
    DateTimeOffset Start { get; set; }
    /// <summary>Gets or sets topic context.</summary>
    [Required]
    DateTimeOffset End { get; set; }

    /// <summary>Gets or sets topic context.</summary>
    string Context { get; set; }

    /// <summary>Gets or sets company identifier.</summary>
    [ForeignKey("Company")]
    _Id CompanyId { get; set; }

    /// <summary>Gets or sets parent category.</summary>
    [ForeignKey("Topic")]
    _Id ParentId { get; set; }
}

#endregion

// ----------------------------------------------------------------------------------------
#region ** abstract entity support

#if false
/// <summary>
/// The field type.
/// </summary>
public enum XFieldType
{
    /// <summary>Integer 128 bit.</summary>
    Integer,
    /// <summary>Big float,</summary>
    Float,
    /// <summary>Start and end dates.</summary>
    Guid,
    /// <summary>Exactly DateTime.</summary>
    DateTime,
    /// <summary>Start and end dates.</summary>
    Period,
    /// <summary>Link (as identifier) to object of entity.</summary>
    Link,
    /// <summary>Link (as identifier) to image.</summary>
    Image,
    /// <summary>Link (as identifier) to text.</summary>
    Text,
    /// <summary>Link (as identifier) to translated text.</summary>
    Translated,
    /// <summary>Link (as identifier) to user.</summary>
    User,
    /// <summary>Link (as identifier) to role.</summary>
    Role,
}

/// <summary>The entity.</summary>
public interface IEntity : IActiveDB, ITitleDB, INoteDB
{
    /// <summary>Gets or sets role identifier.</summary>
    [Required]
    [ForeignKey("Role")]
    _Id RoleId { get; set; }

    /// <summary>Gets or sets topic type.</summary>
    [Required]
    XFieldType Type { get; set; }

    /// <summary>Gets or sets topic context.</summary>
    [Required]
    DateTimeOffset Start { get; set; }
    /// <summary>Gets or sets topic context.</summary>
    [Required]
    DateTimeOffset End { get; set; }

    /// <summary>Gets or sets topic context.</summary>
    string Context { get; set; }

    /// <summary>Gets or sets parent category.</summary>
    [ForeignKey("Entity")]
    _Id ParentId { get; set; }
}

/// <summary>The field.</summary>
public interface IField : IActiveDB, ITitleDB, INoteDB
{
    /// <summary>Gets or sets role identifier.</summary>
    [Required]
    [ForeignKey("Entity")]
    _Id EntityId { get; set; }

    /// <summary>Gets or sets this field type.</summary>
    [Required]
    XFieldType Type { get; set; }

    /// <summary>Gets or sets field context.</summary>
    [Required]
    DateTimeOffset Start { get; set; }
    /// <summary>Gets or sets field context.</summary>
    [Required]
    DateTimeOffset End { get; set; }

    /// <summary>Gets or sets topic context.</summary>
    string Format { get; set; }
}

/// <summary>The object.</summary>
public interface IObject
{
    /// <summary>Gets or sets role identifier.</summary>
    [Key]
    _Id EntityId { get; set; }

    /// <summary>Gets or sets role identifier.</summary>
    [Key]
    _Id FieldId { get; set; }

    /// <summary>Gets or sets topic context.</summary>
    [MaxLength(16)]
    byte[] Value { get; set; }
}
#endif
#endregion
