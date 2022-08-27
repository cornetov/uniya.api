using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;

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
    /// <summary>Gets object identifier.</summary>
    [Key]
    long Id { get; set; }
    /// <summary>Gets UTC date and time of create.</summary>
    [Required]
    DateTime Created { get; set; }
    /// <summary>Gets UTC date and time of last change.</summary>
    [Required]
    DateTime Modified { get; set; }
}

/// <summary>Support timed database object with note.</summary>
public interface ITitleDB : IDB
{
    /// <summary>Gets or sets name.</summary>
    [Required]
    string Name { get; set; }
    /// <summary>Gets or sets title.</summary>
    string Title { get; set; }
    /// <summary>Gets or sets description.</summary>
    string Description { get; set; }
}

/// <summary>Support active and timed object.</summary>
public interface IActiveDB : IDB
{
    /// <summary>Gets or sets whether active or not.</summary>
    [Required]
    bool IsActive { get; set; }
}

/// <summary>Support class name with Run() method as active and timed object.</summary>
public interface IRunDB : IActiveDB
{
    /// <summary>Gets or sets code class name for run.</summary>
    [Required]
    string ClassName { get; set; }
}

/// <summary>Support timed database object with note.</summary>
public interface INoteDB : IDB
{
    /// <summary>Gets or sets note.</summary>
    string Note { get; set; }

    /// <summary>Gets user that created a record.</summary>
    [Required]
    [ForeignKey("User")]
    long CreatedUserId { get; set; }
    /// <summary>Gets user that modified a record.</summary>
    [Required]
    [ForeignKey("User")]
    long ModifiedUserId { get; set; }
}

#endregion

/*********************************************************************************************/
#region ** local database

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
#region ** data

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

/*********************************************************************************************/
#region ** schema

/// <summary>The requirement options of data.</summary>
[Flags]
public enum XRequirementOptions : byte
{
    /// <summary>Not constrain of data.</summary>
    None = 0,
    /// <summary>Business recommended of data.</summary>
    Recommended = 1,
    /// <summary>Business required of data.</summary>
    Required = 2,
    /// <summary>Unique of data.</summary>
    UniqueKey = 4,
    /// <summary>ReadOnly/calculated of data.</summary>
    ReadOnly = 8,
    /// <summary>Not null data.</summary>
    NotNull = 16,
    /// <summary>Table (primary key or foreign key) identifier.</summary>
    ForeignKey = 32,
    /// <summary>System required not null data (complex value).</summary>
    SystemRequired = 19,
    /// <summary>Table required foreign key (complex value).</summary>
    RequiredForignKey = 51,
    /// <summary>Table primary key (complex value).</summary>
    PrimaryKey = 63,
}

/// <summary>
/// Specifies the data type of a field, a property, or a Parameter object.
/// </summary>
public enum XDataType : byte
{
    /// <summary>A type representing Unicode character strings.</summary>
    String = 0,
    /// <summary>A variable-length stream of binary data.</summary>
    Binary = 1,
    /// <summary>An 8-bit unsigned integer ranging in value from 0 to 255.</summary>
    Byte = 2,
    /// <summary>A simple type representing Boolean values of true or false.</summary>
    Boolean = 3,
    /// <summary>A currency value ranging from -2E63 to 2E63 with an accuracy to a ten-thousandth of a currency unit.</summary>
    Currency = 4,
    /// <summary>A type representing a date value.</summary>
    Date = 5,
    /// <summary>A type representing a date and time value.</summary>
    DateTime = 6,
    /// <summary>A simple type representing decimal value with 28-29 significant digits.</summary>
    Decimal = 7,
    /// <summary>A floating point type representing value with a precision of 15-16 digits.</summary>
    Double = 8,
    /// <summary>A globally unique identifier (or GUID).</summary>
    Guid = 9,
    /// <summary>An integral type representing signed 16-bit integers with values between -32768 and 32767.</summary>
    Int16 = 10,
    /// <summary>An integral type representing signed 32-bit integers with values between -2147483648 and 2147483647.</summary>
    Int32 = 11,
    /// <summary>An integral type representing signed 64-bit integers with values between -9223372036854775808 and 9223372036854775807.</summary>
    Int64 = 12,
    /// <summary>A type representing a SQL Server time value.</summary>
    Time = 17,
    /// <summary>A fixed-length string of Unicode characters.</summary>
    [Obsolete]
    StringFixedLength = 22,
    /// <summary>A parsed representation of an XML document or fragment.</summary>
    [Obsolete]
    Xml = 25,
    /// <summary>A parsed representation of an option set.</summary>
    OptionSet = 90,
    /// <summary>A parsed representation of an entity reference.</summary>
    Reference = 91,
    /// <summary>A parsed representation of a entity collection.</summary>
    Array = 92,
    /// <summary>A parsed representation of a unknown type.</summary>
    Unknown = 99,
}

/// <summary>
/// The update or delete type ar references.
/// </summary>
public enum XReferenceType : byte
{
    /// <summary>No action.</summary>
    None = 0,
    /// <summary>Cascade for on update or delete.</summary>
    Cascade = 1,
    /// <summary>Restrict for on delete.</summary>
    Restrict = 2,
    /// <summary>Set null on delete.</summary>
    SetNull = 3,
    /// <summary>Set null on delete.</summary>
    SetDefault = 4,
}

/// <summary>
/// The schema interface of the database.
/// </summary>
public interface ISchema
{
    /// <summary>Gets or sets logical entity name.</summary>
    string SchemaName { get; set; }
    /// <summary>Gets or sets display name of the table.</summary>
    string Title { get; set; }
    /// <summary>Gets or sets the primary value of the entity.</summary>
    string Description { get; set; }

    /// <summary>Gets or sets root name of XML or JSON data.</summary>
    string RootName { get; set; }
    /// <summary>Gets or sets item name of XML or JSON data.</summary>
    string ItemName { get; set; }
    /// <summary>Gets or sets type name of XML or JSON data.</summary>
    string TypeName { get; set; }
    /// <summary>Gets or sets result name of XML or JSON data.</summary>
    string ResultName { get; set; }
    /// <summary>Gets or sets collection name of XML or JSON data.</summary>
    string CollectionName { get; set; }

    /// <summary>Gets or sets the data type for primary and foreign keys of the tables.</summary>
    XDataType KeyType { get; set; }
    /// <summary>Gets or sets the created date and time of the schema.</summary>
    DateTime CreatedTime { get; set; }

    /// <summary>Gets a collection of table schemes.</summary>
    IList<ITableSchema> Tables { get; }
    /// <summary>Gets a collection of one to many relation schemes.</summary>
    IList<IRelationSchema> Relations { get; }

    /// <summary>Gets a table scheme by name.</summary>
    /// <param name="name">The table name.</param>
    /// <returns>The table scheme or <b>null</b>.</returns>
    ITableSchema GetTableByName(string name);
}

/// <summary>
/// The table schema interface of the database.
/// </summary>
public interface ITableSchema
{
    /// <summary>Gets or sets logical entity (table) name with schema name, for example: "dbo.MyTable".</summary>
    string TableName { get; set; }
    /// <summary>Gets or sets the description of the table (entity).</summary>
    string Description { get; set; }

    /// <summary>Gets or sets display name of the table.</summary>
    string Title { get; set; }
    /// <summary>Gets or sets collection display name of the table.</summary>
    string CollectionTitle { get; set; }

    /// <summary>Gets or sets the primary key name of the table (entity).</summary>
    string PrimaryKey { get; set; }
    /// <summary>Gets or sets the parent key name of the table (entity).</summary>
    string ParentKey { get; set; }
    /// <summary>Gets or sets the view .NET format using column name as {%column%} of the table (entity).</summary>
    string ViewFormat { get; set; }

    /// <summary>Gets or sets text relation for the many to many table (entity).</summary>
    /// <remarks>Format of many to many relation: four names for each relation.</remarks>
    string ManyToMany { get; set; }

    /// <summary>
    /// Gets a collection of table column schemes.
    /// </summary>
    IList<IColumnSchema> Columns { get; }

    /// <summary>
    /// Gets a collection of table index schemes.
    /// </summary>
    IList<IIndexSchema> Indexes { get; }

    /// <summary>
    /// Gets a column schema by name.
    /// </summary>
    /// <param name="itemName">The item (attribute) logical name.</param>
    /// <returns>The column schema.</returns>
    IColumnSchema GetColumnSchema(string itemName);
}

/// <summary>
/// The column schema interface of the database.
/// </summary>
public interface IColumnSchema
{
    /// <summary>Gets or sets the logical name of the column of the table.</summary>
    string ColumnName { get; set; }

    /// <summary>Gets or sets display name of the column of the table.</summary>
    string Title { get; set; }
    /// <summary>Gets or sets the description of the column of the table.</summary>
    string Description { get; set; }

    /// <summary>Gets or sets foreign relation name (Requirement as ForeignKey) of the table.</summary>
    string ForeignTable { get; set; }

    /// <summary>Gets or sets type of the object for the column of the table.</summary>
    XDataType DataType { get; set; }
    /// <summary>Gets or sets default value for the column of the table.</summary>
    object DefautValue { get; set; }

    /// <summary>Gets or sets format of the column in regular expression.</summary>
    string Pattern { get; set; }
    /// <summary>Gets or sets visual length in characters, by default -1.</summary>
    int Length { get; set; }
    /// <summary>Gets or sets requirement options of the column of the table.</summary>
    XRequirementOptions Requirement { get; set; }

    /// <summary>Gets option set collection of the column of the table.</summary>
    IList<IOptionSchema> OptionSet { get; }

    /// <summary>Gets or sets logical entity (table) name with schema name of this column.</summary>
    string TableName { get; set; }
}

/// <summary>
/// The relation schema interface of the database.
/// </summary>
public interface IRelationSchema
{
    /// <summary>Gets or sets logical entity (table) name.</summary>
    string RelationName { get; set; }

    /// <summary>The referenced table entity record is changed.</summary>
    XReferenceType Update { get; set; }
    /// <summary>The referenced table (entity) record is deleted.</summary>
    XReferenceType Delete { get; set; }
    /// <summary>The referenced table (entity) record is shared/unshared with another user.</summary>
    XReferenceType Share { get; set; }

    /// <summary>Gets or sets the name of the referenced table (entity).</summary>
    string ToTable { get; set; }
    /// <summary>Gets or sets the name of the referencing table (entity).</summary>
    string FromTable { get; set; }
    /// <summary>Gets or sets the name of the referencing column name (foreign key).</summary>
    string ColumnName { get; set; }
}

/// <summary>
/// The index schema interface of the database.
/// </summary>
public interface IIndexSchema
{
    /// <summary>Gets or sets the logical name of the index of the table.</summary>
    string IndexName { get; set; }
    /// <summary>Gets or sets the description of the index of the table.</summary>
    string Description { get; set; }

    /// <summary>Gets or sets logical entity (table) name with schema name, for example: "dbo.MyTable".</summary>
    string TableName { get; set; }

    /// <summary>
    /// Gets a collection of table column for this index.
    /// </summary>
    IList<IColumnSchema> Columns { get; }
}

/// <summary>
/// The option set interface of the database.
/// </summary>
public interface IOptionSchema
{
    /// <summary>Gets or sets the value of the option set item.</summary>
    int Value { get; set; }
    /// <summary>Gets or sets display name of the option set item.</summary>
    string Title { get; set; }
    /// <summary>Gets or sets the description of the option set item.</summary>
    string Description { get; set; }
}

#endregion
