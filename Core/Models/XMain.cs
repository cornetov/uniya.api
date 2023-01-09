using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

using _Id = System.Guid;

namespace Uniya.Core;

// ----------------------------------------------------------------------------------------
#region ** role support

/// <summary>
/// Defined model roles.
/// </summary>
public static class XRole
{
    /// <summary>Administrator</summary>
    public const string Administrator = "Admin";
    /// <summary>Reader</summary>
    public const string Reader = "Reader";
    /// <summary>Writer</summary>
    public const string Writer = "Writer";
}

/// <summary>Roles.</summary>
public interface IRole : IActiveDB, ITitleDB, INoteDB
{
    /// <summary>Gets or sets parent role.</summary>
    [ForeignKey("Role")]
    _Id ParentId { get; set; }
}

/// <summary>Solutions.</summary>

public interface ISolution : ITitleDB
{
    /// <summary>Gets or sets role identifier.</summary>
    [Required]
    [ForeignKey("Role")]
    _Id RoleId { get; set; }

    /// <summary>Gets or sets parent solution identifier.</summary>
    [Required]
    [ForeignKey("Solution")]
    _Id ParentId { get; set; }
}

#endregion

// ----------------------------------------------------------------------------------------
#region ** person with addresses support

/// <summary>
/// Address type.
/// </summary>
public enum XAddressType
{
    /// <summary>Official country (nation) name.</summary>
    Country,
    /// <summary>Official region/subregion name or number.</summary>
    RegionOrArea,
    /// <summary>Settlement name or number (city, town, village, etc.)</summary>
    Settlement,
    /// <summary>Settlement street/block name or number (avenue, borough, district, alley, etc.).</summary>
    StreetOrBlock,
    /// <summary>Building name or number (house, corpus, section, etc.).</summary>
    Building,
    /// <summary>Apartment name or number (flat, room, studio, etc.).</summary>
    Apartment
}

/// <summary>Address.</summary>
public interface IAddress : IDB
{
    // Gets or sets address type.
    [Required]
    XAddressType Type { get; set; }
    // Gets or sets name or number of this address.
    [Required]
    string Name { get; set; }

    // Gets or sets index (zip code) or prefix of this address.
    string Index { get; set; }
    // Gets or sets phone number or prefix of this address.
    string Phone { get; set; }

    // Gets or sets description of this address.
    string Description { get; set; }

    /// <summary>Gets or sets parent address.</summary>
    [ForeignKey("Address")]
    _Id ParentId { get; set; }
}

/// <summary>Persons.</summary>
public interface IPerson : IDB
{
    // Gets or sets first name of this person.
    [Required]
    string FirstName { get; set; }
    // Gets or sets second name of this person.
    string SecondName { get; set; }
    // Gets or sets last name of this person.
    [Required]
    string LastName { get; set; }

    // Gets or sets birthday of this person.
    DateTime Birthday { get; set; }

    // Gets or sets primary mobile phone number of this person.
    string MobilePhone { get; set; }
    // Gets or sets primary alternative phone number of this person.
    string AlternativePhone { get; set; }

    // Gets or sets primary personal email address of this person.
    string PersonalEmail { get; set; }
    // Gets or sets alternative personal email address of this person.
    string AlternativeEmail { get; set; }

    /// <summary>Gets or sets official address identifier of this person.</summary>
    [ForeignKey("Address")]
    long OfficialAddressId { get; set; }
    /// <summary>Gets or sets real address identifier of this person.</summary>
    [ForeignKey("Address")]
    _Id RealAddressId { get; set; }
    /// <summary>Gets or sets alternative address identifier of this person.</summary>
    [ForeignKey("Address")]
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
    string Name { get; set; }
    /// <summary>Gets or sets name.</summary>
    [Unique]
    string Code { get; set; }
    /// <summary>Gets or sets user's password hash.</summary>
    [Required]
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
    string Phone { get; set; }
    // Gets or sets checked or no primary phone number of this person.
    bool IsPhoneChecked { get; set; }

    // Gets or sets primary email address of this person.
    [Unique]
    string Email { get; set; }
    // Gets or sets checked or no primary email address of this person.
    bool IsEmailChecked { get; set; }

    /// <summary>Gets or sets user's person identifier.</summary>
    [Unique]
    [ForeignKey("Person")]
    _Id PersonId { get; set; }
}

/// <summary>User's roles.</summary>
public interface IUserSession : IActiveDB
{
    /// <summary>Gets or sets user identifier.</summary>
    [Required]
    [ForeignKey("User")]
    _Id UserId { get; set; }
    /// <summary>Gets or sets session refresh GUID.</summary>
    [Required]
    string RefreshGuid { get; set; }
    /// <summary>Gets or sets expires in date and time.</summary>
    [Required]
    DateTime ExpiresIn { get; set; }
    /// <summary>Gets or sets session UTC time.</summary>
    [Required]
    DateTimeOffset LoggedIn { get; set; }
    /// <summary>Gets or sets browser or device fingerprint.</summary>
    string Fingerprint { get; set; }
}

/// <summary>User's roles.</summary>
public interface IUserRole : IActiveDB, INoteDB
{
    /// <summary>Gets or sets user identifier.</summary>
    [Unique("UR")]
    [ForeignKey("User")]
    _Id UserId { get; set; }
    /// <summary>Gets or sets role identifier.</summary>
    [Unique("UR")]
    [ForeignKey("Role")]
    _Id RoleId { get; set; }
}

#endregion

// ----------------------------------------------------------------------------------------
#region ** connections and tasks support

/// <summary>The connection parameters to a server.</summary>
public interface IConnection : ITitleDB, IRunDB, INoteDB
{
    /// <summary>Gets or sets URL of the remote host server.</summary>
    [Required]
    string HostUrl { get; set; }
    ///// <summary>Gets or sets port of the remote host server.</summary>
    //int HostPort { get; set; }

    /// <summary>Gets or sets user name for authenticate in the remote server.</summary>
    string HostUserName { get; set; }
    /// <summary>Gets or sets password for authenticate in the remote server.</summary>
    string HostPassword { get; set; }

    /// <summary>Gets or sets certificate for authenticate with in remote server.</summary>
    string HostCertificate { get; set; }
    /// <summary>Gets or sets complex code (connection string or same added code).</summary>
    string ComplexCode { get; set; }

    /// <summary>Gets or sets extended identifier for the client or other secure text.</summary>
    string SecureText { get; set; }

    /// <summary>Gets or sets URL of the remote server.</summary>
    string ProxyUri { get; set; }
    /// <summary>Gets or sets user name for authenticate in the remote server.</summary>
    string ProxyUserName { get; set; }
    /// <summary>Gets or sets password for authenticate in the remote server.</summary>
    string ProxyPassword { get; set; }

    /// <summary>Gets or sets connector type of the remote server.</summary>
    XConnectorType ConnectorType { get; set; }
    /// <summary>Gets or sets extension data (optional).</summary>
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

public interface IScript : IActiveDB, ITitleDB, INoteDB
{
    /// <summary>Gets or sets script code.</summary>
    [Required]
    string ScriptCode { get; set; }

    /// <summary>Gets or sets role identifier.</summary>
    [Required]
    [ForeignKey("Role")]
    long RoleId { get; set; }
}

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
#region ** entity support

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
    [ForeignKey("Topic")]
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

    /// <summary>Gets or sets topic context.</summary>
    [Required]
    DateTimeOffset Start { get; set; }
    /// <summary>Gets or sets topic context.</summary>
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
    //[Length(16)]
    byte[] Value { get; set; }
}

#endregion
