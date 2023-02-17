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
