
using System;
using System.Xml;
using System.Net;
using System.Text;
using System.Data;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Diagnostics;

namespace Uniya.Core;

// -------------------------------------------------------------------------
#region ** database schema

/// <summary>The database schema.</summary>
[Serializable]
public partial class XSchema : ISchema
{
    /// <summary>
    /// Default initialization.
    /// </summary>
    public XSchema()
    {
        Tables = new XCollection<ITableSchema, string>();
        Relations = new XCollection<IRelationSchema, string>();
        CreatedTime = DateTime.Now;

        SchemaName = RootName = "root";
        ItemName = "item";
        TypeName = "type";
        ResultName = "result";
        CollectionName = "_collection";
    }

    /// <summary>Gets or sets logical entity name.</summary>
    public string SchemaName { get; set; }
    /// <summary>Gets or sets display name of the table.</summary>
    public string Title { get; set; }
    /// <summary>Gets or sets the primary value of the entity.</summary>
    public string Description { get; set; }

    /// <summary>Gets root name of XML or JSON data.</summary>
    public string RootName { get; set; }
    /// <summary>Gets item name of XML or JSON data.</summary>
    public string ItemName { get; set; }
    /// <summary>Gets type name of XML or JSON data.</summary>
    public string TypeName { get; set; }
    /// <summary>Gets result name of XML or JSON data.</summary>
    public string ResultName { get; set; }
    /// <summary>Gets collection name of XML or JSON data.</summary>
    public string CollectionName { get; set; }

    /// <summary>Gets or sets the data type for primary and foreign keys of the tables.</summary>
    public XDataType KeyType { get; set; }
    /// <summary>Gets or sets the created date and time of the schema.</summary>
    public DateTime CreatedTime { get; set; }

    /// <summary>Gets a collection of table schema.</summary>
    public IList<ITableSchema> Tables { get; private set; }
    /// <summary>Gets a collection of one to many relation schema.</summary>
    public IList<IRelationSchema> Relations { get; private set; }

    /// <summary>Gets a table scheme by name.</summary>
    /// <param name="name">The table name.</param>
    /// <returns>The table scheme or <b>null</b>.</returns>
    public ITableSchema GetTableByName(string name)
    {
        for (int i = 0; i < Tables.Count; i++)
        {
            if (name.ToLower().Equals(Tables[i].TableName.ToLower()))
                return Tables[i];
        }
        return null;
    }


    /// <summary>
    /// Returns a string that represents the current object.
    /// </summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
    {
        var name = string.IsNullOrEmpty(SchemaName) ? "NO_NAME_SCHEMA" : SchemaName;
        return $"SCHEMA [{name}:{Tables.Count}]";
    }

    // ** sort tables [boolean descent = false]

    internal void Sort()
    {
        int idx = 0;
        var cache = new Dictionary<string, Dictionary<string, bool>>();
        while (idx < Tables.Count - 1)
        {
            // foreign keys
            Dictionary<string, bool> keys;
            var tableName = Tables[idx].TableName.ToLower();
            if (cache.ContainsKey(tableName))
            {
                keys = cache[tableName];
            }
            else
            {
                keys = new Dictionary<string, bool>();
                foreach (var column in Tables[idx].Columns)
                {
                    var foreignTable = (column.ForeignTable != null) ? column.ForeignTable.ToLower() : string.Empty;
                    if ((column.Requirement & XRequirementOptions.ForeignKey) != 0 && !keys.ContainsKey(foreignTable))
                    {
                        var required = (column.Requirement & XRequirementOptions.Required) != 0
                            || (column.Requirement & XRequirementOptions.Recommended) != 0
                            || (column.Requirement & XRequirementOptions.UniqueKey) != 0;
                        if (foreignTable.StartsWith("["))
                        {
                            foreach (var s in foreignTable.Split('[', ']', ',', ';'))
                            {
                                if (string.IsNullOrWhiteSpace(s) || keys.ContainsKey(s))
                                    continue;
                                keys.Add(s, false);
                            }
                        }
                        else if (!string.IsNullOrWhiteSpace(foreignTable) && !keys.ContainsKey(foreignTable))
                        {
                            keys.Add(foreignTable, required);
                        }
                    }
                }
                cache.Add(tableName, keys);
            }

            // swap?
            if (keys.Count > 0)
            {
                for (int i = idx + 1; i < Tables.Count; i++)
                {
                    // logical name of the other table
                    var cross = false;
                    var otherName = Tables[i].TableName.ToLower();
                    foreach (var column in Tables[i].Columns)
                    {
                        if ((column.Requirement & XRequirementOptions.ForeignKey) != 0)
                        {
                            var foreignTable = (column.ForeignTable != null) ? column.ForeignTable.ToLower() : string.Empty;
                            if (tableName.Equals(foreignTable))
                            {
                                // already sorted?
                                cross = ((column.Requirement & XRequirementOptions.Required) != 0
                                    || (column.Requirement & XRequirementOptions.Recommended) != 0
                                    || (column.Requirement & XRequirementOptions.UniqueKey) != 0);
                                break;
                            }
                        }
                    }

                    if (!cross && keys.ContainsKey(otherName))
                    {
                        if (!keys[otherName] && cache.ContainsKey(otherName))
                        {
                            var otherKeys = cache[otherName];
                            if (otherKeys.ContainsKey(tableName) && otherKeys[tableName])
                                continue;
                            if (otherKeys.Count >= keys.Count)
                                continue;
                        }

                        // swap of tables
                        var swapTable = Tables[idx];
                        Tables[idx] = Tables[i];
                        Tables[i] = swapTable;
                        idx--;
                        break;
                    }
                }
            }
            idx++;
        }
    }

    // ** static

    /// <summary>
    /// Gets schema name and simple table name.
    /// </summary>
    /// <param name="tableName">The complex (with schema) table name.</param>
    /// <returns>The schema name and simple table name.</returns>
    public static XSchemaInfo GetInfo(string tableName)
    {
        var info = new XSchemaInfo() { Schema = string.Empty, Table = tableName.Trim() };
        var ss = tableName.Split('.');
        if (ss.Length > 1)
        {
            info.Schema = ss[0].Trim();
            info.Table = tableName.Substring(ss[0].Length).Trim();
        }
        return info;
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="value1"></param>
    /// <param name="value2"></param>
    /// <returns></returns>
    public static bool IsEqualTypes(object value1, object value2)
    {
        if (value1 != null && value2 != null)
            return IsEqualTypes(GetDataType(value1.GetType()), GetDataType(value2.GetType()));
        return true;
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="dataType"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public static bool IsEqualTypes(XDataType dataType, object value)
    {
        if (value != null)
            return IsEqualTypes(dataType, GetDataType(value.GetType()));
        return true;
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="dataType1"></param>
    /// <param name="dataType2"></param>
    /// <returns></returns>
    public static bool IsEqualTypes(XDataType dataType1, XDataType dataType2)
    {
        if (dataType1 == dataType2)
            return true;
        if (dataType1 == XDataType.Int64)
            return (dataType2 == XDataType.Int32 || dataType2 == XDataType.Int16);
        if (dataType1 == XDataType.Int32)
            return (dataType2 == XDataType.Int16);
        return false;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static XDataType GetDataType(Type type)
    {
        // ---------------------------------------------------------
        #region ** data type code

        if (type == typeof(byte[]))
            return XDataType.Binary;
        if (type == typeof(bool))
            return XDataType.Boolean;
        if (type == typeof(byte))
            return XDataType.Byte;
        //if (type )
        //    return XDataType.Currency;
        //if (type )
        //    return XDataType.Date;
        if (type == typeof(DateTime))
            return XDataType.DateTime;
        if (type == typeof(decimal))
            return XDataType.Decimal;
        if (type == typeof(float) || type == typeof(double))
            return XDataType.Double;
        if (type == typeof(Guid))
            return XDataType.Guid;
        //if (type == typeof(short) || type == typeof(ushort))
        //    return XDataType.Int16;
        if (type == typeof(short) || type == typeof(ushort) || type == typeof(int) || type == typeof(uint))
            return XDataType.Int32;
        if (type == typeof(long) || type == typeof(ulong))
            return XDataType.Int64;
        //if (type )
        //    return XDataType.Internal;
        if (type == typeof(string))
            return XDataType.String;
        //if (type == typeof(char[]))
        //    return XDataType.StringFixedLength;
        //if (type is )
        //    return XDataType.Time;
        if (type == typeof(XmlDocument))
            //return XDataType.Xml;
            return XDataType.String;

        #endregion

        // enumerations
        if (type.IsEnum)
        {
            return XDataType.Int32;
        }

        // list
        if (type.IsGenericType)
        {
            return XDataType.String;
        }

        // option set?
        if (type == typeof(XOptionSetValue))
        {
            return XDataType.OptionSet;
        }

        // reference?
        if (type == typeof(XEntityReference))
        {
            return XDataType.Reference;
        }

        // array?
        if (type == typeof(XEntityCollection))
        {
            return XDataType.Array;
        }

        // default
#if DEBUGx
        Debug.Assert(type.IsSubclassOf(typeof(Diadoc.Api.SafeComObject)), "strange column type!");
#endif
        return XDataType.String;
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="dataType"></param>
    /// <returns></returns>
    public static Type GetType(XDataType dataType)
    {
        switch (dataType)
        {
            case XDataType.Binary: return typeof(byte[]);
            case XDataType.Boolean: return typeof(bool);
            case XDataType.Byte: return typeof(byte);
            case XDataType.Currency: return typeof(double);
            case XDataType.Date: return typeof(DateTime);
            case XDataType.DateTime: return typeof(DateTime);
            case XDataType.Decimal: return typeof(decimal);
            case XDataType.Double: return typeof(double);
            case XDataType.Guid: return typeof(Guid);
            //case XDataType.Int16: return typeof(short);
            case XDataType.Int32: return typeof(int);
            case XDataType.Int64: return typeof(long);
            case XDataType.String: return typeof(string);
            //case XDataType.Time: return typeof(DateTime);
            //case XDataType.Xml: return typeof(XmlDocument);
        }

        // default
        Debug.Assert(false, "strange column type!");
        return typeof(object);
    }
}

#endregion

// -------------------------------------------------------------------------
#region ** relation (one to many) schema

/// <summary>The one to many relation of schema.</summary>
[Serializable]
public partial class XRelationSchema : IRelationSchema
{
    /// <summary>
    /// Default initialization.
    /// </summary>
    public XRelationSchema()
    {
        Delete = XReferenceType.Restrict;
    }

    /// <summary>Gets or sets logical entity (table) name.</summary>
    public string RelationName { get; set; }
    ///// <summary>Gets or sets the schema name of the ralation.</summary>
    //public string SchemaName { get; set; }

    /// <summary>The referenced table entity record is changed.</summary>
    public XReferenceType Update { get; set; }
    /// <summary>The referenced table (entity) record is deleted.</summary>
    public XReferenceType Delete { get; set; }
    /// <summary>The referenced table (entity) record is shared/unshared with another user.</summary>
    public XReferenceType Share { get; set; }

    /// <summary>Gets or sets the name of the referenced table (entity).</summary>
    public string ToTable { get; set; }
    /// <summary>Gets or sets the name of the referencing table (entity).</summary>
    public string FromTable { get; set; }
    /// <summary>Gets or sets the name of the referencing column name (foreign key).</summary>
    public string ColumnName { get; set; }

    /// <summary>
    /// Returns a string that represents the current object.
    /// </summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
    {
        return $"RELATION [{RelationName}:{FromTable}/{ColumnName}->{ToTable}]";
    }
}

#endregion

// -------------------------------------------------------------------------
#region ** table schema

/// <summary>
/// The table information for schema of the database.
/// </summary>
public class XSchemaInfo
{
    /// <summary>Gets or sets logical entity (table) name with schema name, for example: "dbo.MyTable".</summary>
    public string Schema { get; set; }
    /// <summary>Gets or sets logical entity (table) name with schema name, for example: "dbo.MyTable".</summary>
    public string Table { get; set; }
}

/// <summary>The table schema.</summary>
[Serializable]
public partial class XTableSchema : ITableSchema
{
    /// <summary>
    /// Initializes table as the entity with the logical name.
    /// </summary>
    public XTableSchema()
    {
        Columns = new ObservableCollection<IColumnSchema>();
        Indexes = new ObservableCollection<IIndexSchema>();
    }

    /// <summary>Gets or sets logical entity (table) name.</summary>
    public string TableName { get; set; }

    /// <summary>Gets or sets the description of the table (entity).</summary>
    public string Description { get; set; }

    /// <summary>Gets or sets display name of the table.</summary>
    public string Title { get; set; }
    /// <summary>Gets or sets collection display name of the table.</summary>
    public string CollectionTitle { get; set; }

    /// <summary>Gets or sets the primary key name of the table (entity).</summary>
    public string PrimaryKey { get; set; }
    /// <summary>Gets or sets the parent key name of the table (entity).</summary>
    public string ParentKey { get; set; }
    /// <summary>Gets or sets the view .NET format using column name as {%column%} of the table (entity).</summary>
    public string ViewFormat { get; set; }

    /// <summary>Gets or sets text relation for the many to many table (entity).</summary>
    /// <remarks>Format of many to many relation: four names for each relation.</remarks>
    public string ManyToMany { get; set; }

    /// <summary>
    /// Gets a collection of table column schema.
    /// </summary>
    public IList<IColumnSchema> Columns { get; private set; }

    /// <summary>
    /// Gets a collection of table index schema.
    /// </summary>
    public IList<IIndexSchema> Indexes { get; private set; }

    /// <summary>Gets or sets the schema name of the column of the table.</summary>
    public string SchemaName { get; set; }

    /// <summary>
    /// Gets a column schema by name.
    /// </summary>
    /// <param name="itemName">The item (attribute) logical name.</param>
    /// <returns>The column schema.</returns>
    public IColumnSchema GetColumnSchema(string itemName)
    {
        foreach (var column in this.Columns)
        {
            if (itemName.Equals(column.ColumnName))
            {
                return column;
            }
            if (itemName.Equals(column.ColumnName.ToLower()))
            {
                return column;
            }
        }
        return null;
    }

    /// <summary>
    /// Returns a string that represents the current object.
    /// </summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
    {
        return $"TABLE [{TableName}:{Columns.Count}]";
    }

    ///// <summary>
    ///// Werther is column name in the table or no.
    ///// </summary>
    ///// <param name="name">A name.</param>
    ///// <returns><b>true</b> if column name, overwise <b>false</b>.</returns>
    //public bool IsColumnName(string name)
    //{
    //    return ((ObservableCollection<IColumnSchema>)Columns).C.
    //}

    /// <summary>
    /// Get primary (almost unique for table) column for search using <see cref="ViewFormat"/> property.
    /// </summary>
    /// <returns>The primary (almost unique for table) column.</returns>
    public IColumnSchema GetPrimaryColumn()
    {
        // parse view format
        IColumnSchema primaryColumn = null;
        var format = ViewFormat;
        var formats = new List<string>();
        if (!string.IsNullOrEmpty(format))
        {
            int start = 0;
            while (true)
            {
                int begin = format.IndexOf("{%", start);
                if (begin < 0) break;
                int end = format.IndexOf("%}", begin);
                if (end <= begin + 2) break;
                formats.Add(format.Substring(begin + 2, end - begin - 2));
                start = end;
            }
        }

        // search primary column and primary attribute for the entity
        foreach (var column in Columns)
        {
            if (formats.Contains(column.ColumnName))
            {
                primaryColumn = column;
                break;
            }
            if (formats.Contains(column.ColumnName.ToLower()))
            {
                primaryColumn = column;
                break;
            }
            if (primaryColumn == null && column.DataType == XDataType.String)
            {
                primaryColumn = column;
            }
        }

        // done
        return primaryColumn;
    }
}

///// <summary>
///// Contains a collection of table names for many to many relation for schemas.
///// </summary>
//public partial class XManyToManyTableCollection : ObservableCollection<string>
//{
//}

/// <summary>
/// Contains a collection of table schema.
/// </summary>
public partial class XTableSchemaCache : ObservableCollection<KeyValuePair<string, XTableSchema>>
{
}

#endregion

// -------------------------------------------------------------------------
#region ** table column schema

/// <summary>The table column schema.</summary>
[Serializable]
public partial class XColumnSchema : IColumnSchema
{
    /// <summary>
    /// Initializes column of the entity with the logical name.
    /// </summary>
    public XColumnSchema()
    {
        OptionSet = new ObservableCollection<IOptionSchema>();
        Length = -1;
        Order = -1;
    }

    /// <summary>Gets or sets the logical name of the column of the table.</summary>
    public string ColumnName { get; set; }
    /// <summary>Gets or sets display name of the column of the table.</summary>
    public string Title { get; set; }
    /// <summary>Gets or sets the description of the column of the table.</summary>
    public string Description { get; set; }

    /// <summary>Gets or sets foreign relation name (Requirement as ForeignKey) of the table.</summary>
    public string ForeignTable { get; set; }

    /// <summary>Gets or sets type of the object for the column of the table.</summary>
    public XDataType DataType { get; set; }
    /// <summary>Gets or sets default value for the column of the table.</summary>
    public object DefautValue { get; set; }

    /// <summary>Gets or sets format of the column in regular expression.</summary>
    public string Pattern { get; set; }
    /// <summary>Gets or sets visual length in characters, by default -1.</summary>
    public int Length { get; set; }
    /// <summary>Gets or sets visual order, by default -1.</summary>
    public int Order { get; set; }
    /// <summary>Gets or sets requirement options of the column of the table.</summary>
    public XRequirementOptions Requirement { get; set; }

    /// <summary>Gets option set collection of the column of the table.</summary>
    public IList<IOptionSchema> OptionSet { get; private set; }

    /// <summary>Gets or sets logical entity (table) name.</summary>
    public string TableName { get; set; }

    /// <summary>
    /// Returns a string that represents the current object.
    /// </summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
    {
        return $"COLUMN [{ColumnName}:{DataType}]";
    }
}

#endregion

// -------------------------------------------------------------------------
#region ** table index schema

/// <summary>The table column schema.</summary>
[Serializable]
public partial class XIndexSchema : IIndexSchema
{
    /// <summary>
    /// Default initialization.
    /// </summary>
    public XIndexSchema()
    {
        Columns = new ObservableCollection<IColumnSchema>();
    }

    /// <summary>Gets or sets the schema name of the column of the table.</summary>
    public string SchemaName { get; set; }
    /// <summary>Gets or sets logical entity (table) name.</summary>
    public string TableName { get; set; }

    /// <summary>Gets or sets the logical name of the index of the table.</summary>
    public string IndexName { get; set; }
    /// <summary>Gets or sets the description of the index of the table.</summary>
    public string Description { get; set; }

    /// <summary>
    /// Gets a collection of schema's table column.
    /// </summary>
    public IList<IColumnSchema> Columns { get; private set; }

    /// <summary>
    /// Returns a string that represents the current object.
    /// </summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
    {
        var sb = new StringBuilder(IndexName);
        if (sb.Length > 0)
        {
            sb.Append('_').Append(TableName);
            foreach (var column in Columns)
            {
                sb.Append('_').Append(column.ColumnName);
            }
        }
        return $"INDEX [{sb}]";
    }
}

#endregion

// -------------------------------------------------------------------------
#region ** option set schema

/// <summary>The option set unit.</summary>
[Serializable]
public partial class XOptionSchema : IOptionSchema
{
    /// <summary>
    /// Default initialization.
    /// </summary>
    public XOptionSchema()
    {
    }

    /// <summary>Gets or sets the value of the option set item.</summary>
    public int Value { get; set; }
    /// <summary>Gets or sets display name of the option set item.</summary>
    public string Title { get; set; }
    /// <summary>Gets or sets the description of the option set item.</summary>
    public string Description { get; set; }

    /// <summary>
    /// Returns a string that represents the current object.
    /// </summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
    {
        return string.Format("{0}|{1}", Value, Title);
    }
}

#endregion

// -------------------------------------------------------------------------
#region ** exceptions

/// <summary>The option set unit.</summary>
public class XSchemaException : Exception
{
    /// <summary>
    /// Uniya schema exception
    /// </summary>
    /// <param name="message">The message about exception.</param>
    /// <param name="name">Incorrect name.</param>
    /// <param name="type">Incorrect type.</param>
    public XSchemaException(string message, string name = null, XDataType type = XDataType.Unknown)
        : base(message)
    {
        Name = string.IsNullOrWhiteSpace(name) ? string.Empty : name;
        Type = type;
    }

    /// <summary>Gets incorrect type.</summary>
    public XDataType Type { get; private set; }
    /// <summary>Gets incorrect name.</summary>
    public string Name { get; private set; }
}

#endregion
