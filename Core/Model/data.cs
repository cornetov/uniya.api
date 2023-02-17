using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;

#if ID_GUID
using _Id = System.Guid;
#else
using _Id = System.Int64;
#endif

// C# 10
namespace Uniya.Core;

/*********************************************************************************************/
#region ** connector/timed

/// <summary>
/// Support database connector.
/// </summary>
public interface IConnetor
{
    /// <summary>Gets or sets connection object.</summary>
    IConnection Connection { get; set; }
}

/// <summary>
/// Support database object.
/// </summary>
public interface IDB
{
    /// <summary>Gets the object identifier.</summary>
    [Key]
    [Display(Name = "ID", Description = "ID of the object")]
    _Id Id { get; set; }
    /// <summary>Gets UTC date and time of created.</summary>
    [Required]
    [Display(Name = "Created", Description = "UTC date and time of created")]
    DateTime Created { get; set; }
    /// <summary>Gets UTC date and time of modified.</summary>
    [Required]
    [Display(Name = "Modified", Description = "UTC date and time of modified")]
    DateTime Modified { get; set; }
}

/// <summary>Support timed database object with note.</summary>
public interface ITitleDB : IDB
{
    /// <summary>Gets or sets name.</summary>
    [Required]
    [Display(Name = "Name", Description = "Unique name of the object")]
    string Name { get; set; }
    /// <summary>Gets or sets title.</summary>
    [Display(Name = "Title", Description = "Title of the object")]
    string Title { get; set; }
    /// <summary>Gets or sets description.</summary>
    [Display(Name = "Description", Description = "Description of the object")]
    string Description { get; set; }
}

/// <summary>Support active and timed object.</summary>
public interface IActiveDB : IDB
{
    /// <summary>Gets or sets the flag of the active object.</summary>
    [Required]
    [Display(Name = "Active", Description = "Flag of the active object")]

    bool IsActive { get; set; }
}

/// <summary>Support class name with Run() method as active and timed object.</summary>
public interface IRunDB : IActiveDB
{
    /// <summary>Gets or sets code class name for run.</summary>
    [Required]
    [Display(Name = "Class name", Description = "Unique class name for run")]
    string ClassName { get; set; }
}

/// <summary>Support timed database object with note.</summary>
public interface INoteDB : IDB
{
    /// <summary>Gets or sets note.</summary>
    [Display(Name = "Note", Description = "Note for the object")]
    string Note { get; set; }

    /// <summary>Gets user who created the object.</summary>
    [Required]
    [Display(Name = "Creator ID", Description = "ID of the user who created the object")]
    [ForeignKey("User")]
    _Id CreatedUserId { get; set; }
    /// <summary>Gets user who modified the object.</summary>
    [Required]
    [Display(Name = "Modifier ID", Description = "ID of the user who modified the object")]
    [ForeignKey("User")]
    _Id ModifiedUserId { get; set; }
}

#endregion

/*********************************************************************************************/
#region ** local database

/// <summary>The type of a connector.</summary>
public enum XConnectorType
{
    /// <summary>Exist ODATA service.</summary>
    OData,
    /// <summary>Microsoft SQL Server.</summary>
    MsSql,
    /// <summary>SQLite server.</summary>
    SQLite,
    /// <summary>PostgreSQL server.</summary>
    PostgreSql,
}

/// <summary>
/// Support local database.
/// </summary>
public interface ILocalDb
{
    /// <summary>Gets exist whether database or no.</summary>
    bool IsExist { get; }
    /// <summary>Gets or sets name of database and file.</summary>
    string Name { get; set; }
    /// <summary>Gets design data interface.</summary>
    ITransactedData Data { get;}

    /// <summary>
    /// Create new database file using database name.
    /// </summary>
    /// <param name="deleteIfExists">Whether delete exist database or no.</param>
    /// <returns><b>true</b> if created, otherwise <b>false</b>.</returns>
    Task<bool> Create(bool deleteIfExists = false);
    /// <summary>
    /// Delete exist database file.
    /// </summary>
    /// <returns><b>true</b> if deleted, otherwise <b>false</b>.</returns>
    Task<bool> Delete();

    /// <summary>
    /// Run SQL script on the database.
    /// </summary>
    /// <param name="script">The SQL script/</param>
    /// <returns>The asynchrony task.</returns>
    Task RunScript(string script);

    /// <summary>Gets script code of some model.</summary>
    /// <param name="schema">The schema of the model.</param>
    /// <returns>The SQL initialization script.</returns>
    string GetSqlScript(ISchema schema);
}

#endregion

/*********************************************************************************************/
#region ** data main

public interface IEntitySet
{
    /// <summary>Creating entities.</summary>
    ICollection<XEntity> Creating { get; }
    /// <summary>Updating entities.</summary>
    ICollection<XEntity> Updating { get; }
    /// <summary>Deleting entities with clear deleted lists.</summary>
    ICollection<XEntity> Deleting { get; }
}

/// <summary>
/// Support read-only database.
/// </summary>
public interface IReadonlyData
{
    // -------------------------------------------------------------------------------
    #region ** select

    /// <summary>
    /// Read one entity using identifier with all columns.
    /// </summary>
    /// <param name="entityName">The entity name.</param>
    /// <param name="pairs">The pair of column name and value.</param>
    /// <returns>The entity collection.</returns>
    Task<XEntityCollection> Read(string entityName, params KeyValuePair<string, object>[] pairs);

    /// <summary>
    /// Select data using query object.
    /// </summary>
    /// <param name="query">The query object.</param>
    /// <returns>The entity collection.</returns>
    Task<XEntityCollection> Select(XQuery query);

    #endregion

    // -------------------------------------------------------------------------------
    #region ** schema

    /// <summary>
    /// Gets schema of the data.
    /// </summary>
    /// <param name="tableNames">The list of used table's names.</param>
    /// <returns>The schema of the data.</returns>
    Task<ISchema> GetSchema(params string[] tableNames);

    #endregion
}

/// <summary>
/// Support CRUD (Create, Read, Update, Delete) methods for databases.
/// </summary>
public interface ICrudData : IReadonlyData
{
    //-----------------------------------------------------------------------------
    #region ** create

    /// <summary>
    /// Create entity object in database.
    /// </summary>
    /// <param name="entities">The collection of entity.</param>
    /// <returns>Without information.</returns>
    Task Create(params XEntity[] entities);

    #endregion

    // -------------------------------------------------------------------------------
    #region ** update

    /// <summary>
    /// Update entity object in database.
    /// </summary>
    /// <param name="entities">The collection of entity.</param>
    /// <returns>Without information.</returns>
    Task Update(params XEntity[] entities);

    #endregion

    // -------------------------------------------------------------------------------
    #region ** delete

    /// <summary>
    /// Delete entity object in database.
    /// </summary>
    /// <param name="entities">The collection of entity.</param>
    /// <returns>Without information.</returns>
    Task Delete(params XEntity[] entities);

    /// <summary>
    /// Delete entity object in database.
    /// </summary>
    /// <param name="entityName">The entity name.</param>
    /// <param name="key">The primary key name.</param>
    /// <param name="ids">The collection of identifiers.</param>
    /// <returns>Without information.</returns>
    Task Delete(string entityName, string key, params object[] ids);

    #endregion
}

/// <summary>
/// Support transacted CRUD (Create, Read, Update, Delete) methods for databases.
/// </summary>
public interface ITransactedData : ICrudData
{
    //-----------------------------------------------------------------------------
    #region ** transaction

    /// <summary>
    /// Apply transaction in database.
    /// </summary>
    /// <param name="iset">The entity set for transaction.</param>
    /// <returns>Without information.</returns>
    Task Transaction(IEntitySet iset);

    #endregion
}

/// <summary>
/// Support CRUD (Create, Read, Update, Delete) methods for databases.
/// </summary>
public interface IDesignData : ICrudData
{
    // -------------------------------------------------------------------------------
    #region ** design

    /// <summary>
    /// Sets table schema of the database.
    /// </summary>
    /// <param name="tableSchema">The new or changed schema of the table.</param>
    /// <returns>The changed schema of the data.</returns>
    Task<ISchema> SetSchema(ITableSchema tableSchema);

    #endregion
}

#endregion
