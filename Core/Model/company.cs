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
