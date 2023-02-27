using System;
using System.IO;
using System.Net;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Text;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Dynamic;
using System.Globalization;
using System.Reflection;
using Microsoft.VisualBasic;
using System.Data;
using System.Linq;
using System.Xml;

namespace Uniya.Core;

// ----------------------------------------------------------------------------------------
#region ** enumerations, flags and interfaces

/// <summary>The current entity state.</summary>
public enum XEntityState : byte
{
    /// <summary>Actual record from the server.</summary>
    Actual = 0,
    /// <summary>Created record (not exist in the server) .</summary>
    Created = 1,
    /// <summary>Modified record (not actual, difference in the server).</summary>
    Modified = 2
}

/// <summary>The entity reference interface.</summary>
public interface IEntityReference
{
    /// <summary>Gets  identifier of record.</summary>
    string Id { get; }
    /// <summary>Gets or sets logical entity name.</summary>
    string EntityName { get; set; }
}

#endregion

/// <summary>The entity of server (table record, list item ...).</summary>
[Serializable]
public class XEntity : DynamicObject
{
    // ------------------------------------------------------------------------------------
    #region ** fields & constructor

    //string _id;
    private string _entityName;
    //static ObservableCollection<KeyValuePair<string, string>> _names = new ObservableCollection<KeyValuePair<string, string>>();
    //static ObservableCollection<KeyValuePair<string, string>> _names = new ObservableCollection<KeyValuePair<string, string>>();

    /// <summary>
    /// Default initialization.
    /// </summary>
    protected XEntity()
    {
        this.Items = new XItemCollection();
        this.OldItems = new XItemCollection();
        this.Children = new List<XEntity>();
    }
    /// <summary>
    /// Initialization using logical name of entities.
    /// </summary>
    /// <param name="entityName">The logical name of entities.</param>
    public XEntity(string entityName)
        : this()
    {
        if (entityName == null)
        {
            throw new ArgumentNullException(nameof(entityName));
        }
        var collection = XSet.Schema.Tables as XCollection<ITableSchema, string>;
        var tableSchema = collection.GetBy(entityName);
        if (tableSchema == null)
        {
            // without schema
            _entityName = entityName;
        }
        else
        {
            // using table schema
            Schema = tableSchema;
        }
        this.State = XEntityState.Created;
    }
    /// <summary>
    /// Initialization using logical name of entities.
    /// </summary>
    /// <param name="schema">The table schema of the entity.</param>
    public XEntity(ITableSchema schema)
        : this()
    {
        this.Schema = schema ?? throw new ArgumentNullException("schema");
        this.State = XEntityState.Created;
    }

    #endregion

    // ------------------------------------------------------------------------------------
    #region ** dynamic object

    /// <summary>
    /// If you try to get a value of a property not defined in the class, this method is called.
    /// </summary>
    /// <param name="binder"></param>
    /// <param name="result"></param>
    /// <returns></returns>
    public override bool TryGetMember(GetMemberBinder binder, out object result)
    {
        return Items.TryGetValue(binder.Name, out result);
    }

    /// <summary>
    /// If you try to set a value of a property that is not defined in the class, this method is called.
    /// </summary>
    /// <param name="binder"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public override bool TrySetMember(SetMemberBinder binder, object value)
    {
        // property name
        var itemName = binder.Name;

        // schema sanity
        if (this.Schema != null)
        {
            var column = this.Schema.GetColumnSchema(itemName);
            if (column == null)
            {
                // bad done
                return false;
            }
            if (value != null && !XSchema.IsEqualTypes(column.DataType, value))
            {
                // bad done
                return false;
            }
        }

        // done
        return SetItemValue(itemName, value);
    }

    /// <summary>
    /// Gets or sets item (attribute) value.
    /// </summary>
    /// <param name="itemName">The item (attribute) name.</param>
    /// <returns>The value (may be <b>null</b> value).</returns>
    public object this[string itemName]
    {
        get { return Items.GetItem(itemName); }
        set
        {
            if (!SetItemValue(itemName, value))
            {
                throw new ArrayTypeMismatchException($"Incorrect column name: '{itemName}' or value data type.");
            }
        }
    }
    /// <summary>
    /// Gets item (attribute) value using a item (attribute) name.
    /// </summary>
    /// <typeparam name="T">The type of item (attribute).</typeparam>
    /// <param name="itemName">The item (attribute) logical name.</param>
    /// <returns>The item (attribute) value.</returns>
    public T GetItemValue<T>(string itemName)
    {
        object value = null;
        if (Items.TryGetValue(itemName, out value) && value != null && !(value is DBNull))
        {
            try
            {
                if (typeof(T) != value.GetType())
                {
                    if (typeof(T) == typeof(bool))
                        return (T)(object)Convert.ToBoolean(value);
                    if (typeof(T) == typeof(decimal))
                        return (T)(object)Convert.ToDecimal(value);
                    if (typeof(T) == typeof(float))
                        return (T)(object)Convert.ToSingle(value);
                    if (typeof(T) == typeof(double))
                        return (T)(object)Convert.ToDouble(value);
                    if (typeof(T) == typeof(long))
                        return (T)(object)Convert.ToInt64(value);
                    return (T)XProxy.GetValue(value.ToString());
                }
                return (T)value;
            }
            catch { }
        }
        return default(T);
    }
    /// <summary>
    /// Sets item (attribute) value.
    /// </summary>
    /// <param name="itemName">The item (attribute) logical name.</param>
    /// <param name="value">The value (may be <b>null</b> value).</param>
    /// <returns><b>true</b> if setting done, otherwise <b>false</b>.</returns>
    protected bool SetItemValue(string itemName, object value)
    {
        // sanity item name
        if (string.IsNullOrWhiteSpace(itemName))
        {
            // bad done
            return false;
        }

        // schema sanity
        if (this.Schema != null)
        {
            var column = this.Schema.GetColumnSchema(itemName);
            if (column == null)
            {
                // bad done
                return false;
            }
            itemName = column.Name;
            if (value != null && !XSchema.IsEqualTypes(column.DataType, value))
            {
                // bad done
                return false;
            }
        }

        // sanity if new value equal old value
        var old = Items.GetItem(itemName);
        if (old == value) return true;
        if (old != null)
        {
            if (old.Equals(value)) return true;
            if (value != null && old.ToString().Equals(value.ToString())) return true;
        }

        // change state and save old value
        if (this.State == XEntityState.Actual)
        {
            this.State = XEntityState.Modified;
        }
        if (this.State == XEntityState.Modified && !OldItems.ContainsKey(itemName))
        {
            OldItems.SetItem(itemName, old);
        }

        // change value
        Items.SetItem(itemName, value);

        // done
        return true;
    }
    #endregion

    // ------------------------------------------------------------------------------------
    #region ** object model

    /// <summary>Gets current entity state.</summary>
    public XEntityState State { get; protected set; }
    /// <summary>Gets current entity state.</summary>
    [JsonIgnore]
    public ITableSchema Schema { get; protected set; }

    /// <summary>Gets the item collection of entity attributes.</summary>
    public XItemCollection Items { get; private set; }
    /// <summary>Gets the item collection of modified attributes with old values.</summary>
    [JsonIgnore]
    public XItemCollection OldItems { get; private set; }
    ///// <summary>Gets formatted collection of the entity.</summary>
    //public XFormattedValueCollection FormattedValues { get; private set; }
    ///// <summary>Gets related collection of the entity.</summary>
    //public XRelatedEntityCollection RelatedEntities { get; private set; }

    /// <summary>Gets internal entities (children).</summary>
    public IList<XEntity> Children { get; private set; }

    /// <summary>Gets or sets entity identifier.</summary>
    public string EntityId
    {
        get
        {
            var value = Items.GetItem(this.PrimaryKey);
            return (value != null) ? value.ToString() : string.Empty;
        }
        set
        {
            var table = GetTable(EntityName);
            var primaryKey = GetPrimaryKey(EntityName, Items, table);
            if (table != null && !string.IsNullOrEmpty(table.PrimaryKey))
            {
                foreach (var column in table.Columns)
                {
                    if (primaryKey.Equals(column.Name))
                    {
                        switch (column.DataType)
                        {
                            case XDataType.Int32:
                                int n;
                                if (int.TryParse(value, out n))
                                    this[primaryKey] = n;
                                else
                                    this[primaryKey] = 0;
                                return;
                            case XDataType.Int64:
                                long l;
                                if (long.TryParse(value, out l))
                                    this[primaryKey] = l;
                                else
                                    this[primaryKey] = 0L;
                                return;
                            case XDataType.Guid:
                                Guid guid;
                                if (Guid.TryParse(value, out guid))
                                    this[primaryKey] = guid;
                                else
                                    this[primaryKey] = Guid.Empty;
                                return;
                            default:
                                Debug.Assert(column.DataType == XDataType.String, "strange identifier type!");
                                break;
                        }
                        break;
                    }
                }
            }

            // text identifier?
            this[primaryKey] = value;
        }
    }

    /// <summary>
    /// Gets primary key name.
    /// </summary>
    public string PrimaryKey
    {
        get { return (Schema != null) ? Schema.PrimaryKey : GetPrimaryKey(EntityName, Items, GetTable(EntityName)); }
    }
    /// <summary>Gets logical name of the entity.</summary>
    public string EntityName
    {
        get { return (Schema != null) ? Schema.Name : _entityName; }
        protected set { _entityName = value; }
    }

    /// <summary>
    /// Convert entity to type.
    /// </summary>
    /// <typeparam name="T">The type of entity.</typeparam>
    /// <returns>The typed object.</returns>
    public T To<T>() where T : class
    {
        // declared interface
        if (typeof(T).IsInterface)
        {
#if !OLD_PROXY
            //if (Schema != null)
            //{

            //}

            var idb = XProxy.Get<T>();
            foreach (PropertyInfo pi in typeof(T).GetPublicProperties())
            {
                var name = pi.Name;
                var value = this[name];
                if (value != null)
                {
                    XProxy.SetValue(pi, idb, value);
                }
            }
            return idb;
#else
            int count = 0;
            foreach (PropertyInfo pi in typeof(T).GetPublicProperties())
            {
                var name = pi.Name;
                var dataType = XSchema.GetDataType(pi.PropertyType);
                if (name.Equals("Id"))
                {
                    if (!long.TryParse(this.EntityId, out long id))
                    {
                        return default(T);
                    }
                    continue;
                }
                if (this.Schema != null)
                {
                    // use table schema
                    var column = this.Schema.GetColumnSchema(name);
                    if (column == null || !XSchema.IsEqualTypes(column.DataType, dataType))
                    {
                        return default(T);
                    }
                }
                else
                {
                    // use current values
                    if (this.Items.ContainsKey(name))
                    {
                        if (XSchema.GetDataType(this[name].GetType()) == dataType)
                            count++;
                        continue;
                    }
                    var ra = (RequiredAttribute)Attribute.GetCustomAttribute(pi, typeof(RequiredAttribute));
                    if (ra != null)
                    {
                        return default(T);
                    }
                }
            }

            // interface done
            return new XProxy(this) as T;
#endif
        }

        // declared class
        var obj = Activator.CreateInstance<T>();
        foreach (var item in Items)
        {
            var type = XProxy.GetType(obj, item.Key);
            if (type != null && item.Value != null)
            {
                if (type == item.Value.GetType())
                {
                    XProxy.SetValue(obj, item.Key, item.Value);
                    continue;
                }

                if (type == typeof(bool))
                {
                    XProxy.SetValue(obj, item.Key, Convert.ToBoolean(item.Value));
                }
                else if (type == typeof(decimal))
                {
                    XProxy.SetValue(obj, item.Key, Convert.ToDecimal(item.Value));
                }
                else if (type == typeof(float))
                {
                    XProxy.SetValue(obj, item.Key, Convert.ToSingle(item.Value));
                }
                else if (type == typeof(double))
                {
                    XProxy.SetValue(obj, item.Key, Convert.ToDouble(item.Value));
                }
                else if (type == typeof(long))
                {
                    XProxy.SetValue(obj, item.Key, Convert.ToInt64(item.Value));
                }
                else
                {
                    XProxy.SetValue(obj, item.Key, XProxy.GetValue(item.Value.ToString()));
                }
            }
        }
        return obj;
    }
    /// <summary>
    /// Convert type to entity using schema.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="schema"></param>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static XEntity From<T>(T obj, ITableSchema schema)
    {
        // sanity
        if (schema == null) throw new ArgumentNullException(nameof(schema));

        // create
        var entity = new XEntity(schema);

        // items
        foreach (var column in schema.Columns)
        {
            var value = XProxy.GetValue(obj, column.Name);
            if (value != null)
            {
                entity[column.Name] = value;
            }
        }

        // done
        return entity;
    }
    public static XEntity From(object obj)
    {
        // initialization
        var type = obj.GetType();
        var entityName = type.Name;
        ITableSchema schema = null;

        // search schema
        if (obj is IDB)
        {
            var tables = (XCollection<ITableSchema, string>)XSet.Schema.Tables;
            schema = tables.GetBy(entityName.Substring(1));
        }

        // create
        var entity = (schema != null) ? new XEntity(schema) : new XEntity(entityName);

        // column cache
        var columns = new Dictionary<string, IColumnSchema>();
        if (schema != null)
        {
            foreach (var column in schema.Columns)
            {
                columns.Add(column.Name, column);
            }
        }

        // set properties
        foreach (PropertyInfo pi in type.GetPublicProperties())
        {
            // initialization
            var name = pi.Name;
            var dataType = XSchema.GetDataType(pi.PropertyType);

            var value = XProxy.GetValue(pi, obj);
            if (value != null)
            {
                if (schema != null)
                {
                    // sanity column name
                    if (!columns.ContainsKey(name))
                    {
                        Console.WriteLine($"In table schema {schema.Name} absent {name}!");
                        continue;
                    }

                    // sanity column type
                    if (dataType != columns[name].DataType)
                    {
                        Console.WriteLine($"In table schema {schema.Name} mismatch types in {name}!");
                        continue;
                    }
                }
                entity[pi.Name] = value;
            }
        }

        // done
        return entity;
    }

    /// <summary>
    /// Clone entity.
    /// </summary>
    /// <param name="entityName">The new entity name if is need.</param>
    /// <param name="fast">Whether is need only rename.</param>
    /// <returns>The new cloned entity.</returns>
    public XEntity Clone(string entityName = null, bool fast = false)
    {
        // all clone
        var zero = string.IsNullOrWhiteSpace(entityName);
        var clone = (!zero && fast) ? this : (XEntity)MemberwiseClone();

        // children
        clone.Children = new List<XEntity>();
        if (!fast)
        {
            foreach (var child in this.Children)
                clone.Children.Add(child.Clone());
        }

        // rename
        if (!zero)
        {
            clone.EntityName = entityName;
        }

        // done
        return clone;
    }

    /// <summary>
    /// Returns a string that represents the current object.B
    /// </summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
    {
        var id = (EntityId != null) ? EntityId : string.Empty;
        var name = (EntityName != null) ? EntityName : string.Empty;
        return string.Format("{0}[{1}]", name, id);
    }

    // ** static block

    /// <summary>
    /// Gets value of the item by name or <b>null</b>.
    /// </summary>
    /// <param name="itemName">The uncased item name.</param>
    /// <returns>The value or <b>null</b>.</returns>
    public object GetItemValue(string itemName)
    {
        if (Items.Count > 0)
        {
            if (Items.ContainsKey(itemName))
                return this[itemName];
            if (Items.ContainsKey(itemName.ToLower()))
                return this[itemName.ToLower()];
        }
        return null;
    }
    /// <summary>
    /// Gets text value of the item by name or empty string.
    /// </summary>
    /// <param name="itemName">The uncased item name.</param>
    /// <returns>The text value or empty string.</returns>
    public string GetItemText(string itemName)
    {
        var value = GetItemValue(itemName);
        return (value != null) ? new XElement("n", value).Value : string.Empty;
    }

    /// <summary>
    /// Actualization data of this entity with set new entity name.
    /// </summary>
    /// <param name="entityName">A new entity name.</param>
    public void Actualization(string entityName = null)
    {
        if (!string.IsNullOrEmpty(entityName))
            this.EntityName = entityName;
        this.State = XEntityState.Actual;
        this.OldItems.Clear();
    }
    /// <summary>
    /// Actualization data of this entity with set new entity name.
    /// </summary>
    /// <param name="schema">A new entity name.</param>
    public bool Actualization(ITableSchema schema)
    {
        foreach (var column in schema.Columns)
        {
            var value = this[column.Name];
            if ((column.Requirement & XRequirementOptions.Required) != 0 && value == null)
            {
                // bad done
                return false;
            }
            if (value != null && !XSchema.IsEqualTypes(column.DataType, value))
            {
                switch (column.DataType)
                {
                    case XDataType.Binary:
                        this[column.Name] = GetItemValue<byte[]>(column.Name);
                        break;
                    case XDataType.Boolean:
                        this[column.Name] = GetItemValue<bool>(column.Name);
                        break;
                    case XDataType.Byte:
                        this[column.Name] = GetItemValue<byte>(column.Name);
                        break;
                    case XDataType.Currency:
                    case XDataType.Double:
                        this[column.Name] = GetItemValue<double>(column.Name);
                        break;
                    case XDataType.Decimal:
                        this[column.Name] = GetItemValue<decimal>(column.Name);
                        break;
                    case XDataType.Guid:
                        this[column.Name] = GetItemValue<Guid>(column.Name);
                        break;
                    case XDataType.Int16:
                    case XDataType.Int32:
                    case XDataType.Int64:
                        this[column.Name] = GetItemValue<long>(column.Name);
                        break;
                    case XDataType.Date:
                    case XDataType.DateTime:
                        this[column.Name] = GetItemValue<DateTime>(column.Name);
                        break;
                    case XDataType.String:
                        this[column.Name] = GetItemText(column.Name);
                        break;
                }
            }
        }

        // set table schema
        Schema = schema;

        // base actualization
        Actualization();

        // done
        return true;
    }

    /// <summary>
    /// Gets found value of the item by name by all hierarchy.</b>.
    /// </summary>
    /// <param name="itemName">The uncased item name.</param>
    /// <returns>The value or <b>null</b>.</returns>
    public object SearchNamedValue(string itemName)
    {
        var value = GetItemValue(itemName);
        if (value != null) return value;
        foreach (var child in Children)
        {
            value = child.SearchNamedValue(itemName);
            if (value != null) return value;
        }
        return null;
    }

    /// <summary>
    /// Whether equal text string in array or no using apriximate method.
    /// </summary>
    /// <param name="text">The string text for search.</param>
    /// <param name="ss">The string array.</param>
    /// <returns>The index equal string or -1.</returns>
    public static int GetEqualIndex(string text, params string[] ss)
    {
        var parts = text.ToLower().Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < ss.Length; i++)
        {
            var keys = ss[i].ToLower().Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != keys.Length) continue;
            int count = 0;
            foreach (var part in parts)
            {
                foreach (var key in keys)
                {
                    if (part.Equals(key))
                    {
                        count++;
                        break;
                    }
                }
                if (count == parts.Length)
                {
                    return i;
                }
            }
        }
        return -1;          // not found
    }

    /// <summary>
    /// Gets data type from text type value.
    /// </summary>
    /// <param name="textType">The text type value.</param>
    /// <returns>The data type.</returns>
    public static XDataType GetDataType(string textType)
    {
        switch (textType.Trim().ToLower())
        {
            case "tinyint": return XDataType.Byte;
            case "smallint": return XDataType.Int16;
            case "int": return XDataType.Int32;
            case "bigint": return XDataType.Int64;
            case "float":
            case "real":
                return XDataType.Double;
            case "uniqueidentifier": return XDataType.Guid;
            //case "geography": return XDataType..Int64;
            case "bit": return XDataType.Boolean;
            case "binary": return XDataType.Binary;
            case "char":
            case "nchar":
            case "varchar":
            case "nvarchar":
            case "text":
            case "ntext":
                return XDataType.String;
            case "decimal":
            case "numeric":
                return XDataType.Decimal;
            case "money":
            case "smallmoney":
                return XDataType.Currency;
            case "datetime":
            case "smalldatetime":
            case "date":
                return XDataType.Date;
            case "time":
            case "timestamp":
                return XDataType.Time;
        }
        Debug.Assert(false, $"strange SQL type: {textType}.");
        return XDataType.Unknown;
    }

    // ** static table block
    static XTableSchemaCache _tableCache = new XTableSchemaCache();
    internal static XTableSchema GetTable(string entityName)
    {
        lock (_tableCache)
        {
            XTableSchema table;
            if (!string.IsNullOrEmpty(entityName) && _tableCache.TryGetValue(entityName, out table))
                return table;
        }
        return null;
    }
    internal static void SetTable(string entityName, XTableSchema table)
    {
        Debug.Assert(entityName.Equals(table.Name) || entityName.Equals(table.Name.ToLower()));
        lock (_tableCache)
        {
            XTableSchema existTable;
            if (_tableCache.TryGetValue(entityName, out existTable))
            {
                // replace
                Debug.Assert(existTable.Name.Equals(table.Name));
                Debug.Assert(existTable.PrimaryKey == table.PrimaryKey);
                _tableCache.SetItem(entityName, table);
            }
            else
            {
                // new cache
                _tableCache.Add(entityName, table);
            }
        }
    }
    internal static string GetPrimaryKey(string entityName, XItemCollection items, XTableSchema table = null)
    {
        // sanity
        var primaryKey = "Id";
        if (!string.IsNullOrEmpty(entityName) && ((items != null && items.Count > 0) || table != null))
        {
            // perhaps primary keys
            var lowerName = entityName.ToLower();
            var primaryKeys = new Dictionary<string, XDataType>();
            primaryKeys.Add("id", XDataType.Unknown);
            primaryKeys.Add(string.Format("{0}id", lowerName), XDataType.Unknown);
            primaryKeys.Add(string.Format("id_{0}", lowerName), XDataType.Unknown);
            primaryKeys.Add(string.Format("{0}_id", lowerName), XDataType.Unknown);
            primaryKeys.Add("activityid", XDataType.Unknown);

            // use table schema
            if (table == null)
            {
                table = GetTable(entityName);
            }
            if (table != null)
            {
                if (string.IsNullOrEmpty(table.PrimaryKey))
                {
                    foreach (var key in primaryKeys.Keys)
                    {
                        foreach (var column in table.Columns)
                        {
                            if (key.Equals(column.Name.ToLower()))
                            {
                                switch (column.DataType)
                                {
                                    case XDataType.Int32:
                                    case XDataType.Int64:
                                    case XDataType.Guid:
                                        break;
                                    default:
                                        Debug.Assert(column.DataType == XDataType.String, "strange primary key!");
                                        break;
                                }
                                table.PrimaryKey = column.Name;
                                return table.PrimaryKey;
                            }
                        }
                    }
                }
            }

            // use entity name
            if (Char.IsLower(entityName, 0))
            {
                Debug.Assert(entityName.Equals(entityName.ToLower()));
                primaryKey = string.Format("{0}id", entityName);
            }
            else
            {
                primaryKey = string.Format("{0}Id", entityName);
            }

            // use items
            if (items != null && items.Count > 0)
            {
                // equal entity name
                if (items.ContainsKey(primaryKey))
                {
                    return primaryKey;
                }

                // search first
                foreach (var item in items)
                {
                    var itemKey = item.Key.ToLower();
                    foreach (var key in primaryKeys.Keys)
                    {
                        if (itemKey.Equals(key))
                        {
                            if (item.Value is int || item.Value is long || item.Value is Guid)
                            {
                                Debug.Assert(itemKey.Equals("id") && XSchema.IsEqualTypes(XDataType.Int64, item.Value));
                                return item.Key;
                            }
                            if (item.Value is string && item.Value.ToString().Length > 0)
                            {
                                Guid guid;
                                if (Guid.TryParse(item.Value.ToString(), out guid))
                                    return item.Key;
                                long n;
                                if (long.TryParse(item.Value.ToString(), out n))
                                    return item.Key;
                            }
                        }
                    }
                }
            }
        }
        return primaryKey;
    }

    internal static string GetParentKey(string entityName, XTableSchema table)
    {
        if (!string.IsNullOrEmpty(entityName))
        {
            if (table == null)
            {
                table = GetTable(entityName);
            }
            if (table != null)
            {
                if (string.IsNullOrEmpty(table.ParentKey))
                {
                    foreach (var column in table.Columns)
                    {
                        var keys = new string[]
                        {
                            "parentid",
                            string.Format("parent{0}id", entityName),
                            string.Format("parentid_{0}", entityName),
                            string.Format("parent_id_{0}", entityName),
                            string.Format("parent_{0}_id", entityName)
                        };
                        foreach (var key in keys)
                        {
                            if (key.ToLower().Equals(column.Name.ToLower()))
                            {
                                switch (column.DataType)
                                {
                                    case XDataType.Int32:
                                    case XDataType.Int64:
                                    case XDataType.Guid:
                                        break;
                                    default:
                                        Debug.Assert(column.DataType == XDataType.String, "strange parent key!");
                                        break;
                                }
                                table.ParentKey = column.Name;
                                return table.ParentKey;
                            }
                        }
                    }
                }
            }
            if (Char.IsLower(entityName, 0))
            {
                Debug.Assert(entityName.Equals(entityName.ToLower()));
                return string.Format("parent{0}id", entityName);
            }
        }
        return "ParentId";
    }
    #endregion

    // ---------------------------------------------------------------------
    #region ** reflection

    #endregion

    // ---------------------------------------------------------------------
    #region ** static merge (sets) methods

    /// <summary>
    /// Merge text item.
    /// </summary>
    /// <param name="from">The from entity.</param>
    /// <param name="fromItem">The from item name.</param>
    /// <param name="to">The to entity.</param>
    /// <param name="toItem">The to item name.</param>
    /// <param name="byDefault">The default value.</param>
    /// <returns><b>true</b> if modified, otherwise <b>false</b>.</returns>
    public static bool SetTextItem(XEntity from, string fromItem, XEntity to, string toItem, string byDefault = "")
    {
        Debug.Assert(!string.IsNullOrEmpty(fromItem) && !string.IsNullOrEmpty(toItem));
        var data = from.GetItemText(fromItem).Trim();
        var text = to.GetItemText(toItem).Trim();
        if (data == string.Empty && text == byDefault)
        {
            return false;
        }
        if (!data.Equals(text))
        {
            to[toItem] = data;
            return true;
        }
        if (text.Length == 0 && text != byDefault)
        {
            to[toItem] = byDefault;
            return true;
        }
        return false;
    }
    /// <summary>
    /// Merge integer item.
    /// </summary>
    /// <param name="from">The from entity.</param>
    /// <param name="fromItem">The from item name.</param>
    /// <param name="to">The to entity.</param>
    /// <param name="toItem">The to item name.</param>
    /// <param name="modified">Set <b>true</b> if modified.</param>
    /// <param name="byDefault">The default value.</param>
    /// <returns><b>true</b> if modified, otherwise <b>false</b>.</returns>
    public static bool SetIntegerItem(XEntity from, string fromItem, XEntity to, string toItem, int byDefault = 0)
    {
        Debug.Assert(!string.IsNullOrEmpty(fromItem) && !string.IsNullOrEmpty(toItem));
        var data = from.GetItemText(fromItem).Trim();
        var text = to.GetItemText(toItem).Trim();
        int result, value;
        if (data.Length > 0 && !data.Equals(text) && int.TryParse(data, out result))
        {
            if (text.Length > 0 && int.TryParse(text, out value) && result == value)
                return false;
            to[toItem] = result;
            return true;
        }
        if (text.Length == 0 && byDefault != 0)
        {
            to[toItem] = byDefault;
            return true;
        }
        return false;
    }
    /// <summary>
    /// Merge integer item.
    /// </summary>
    /// <param name="from">The from entity.</param>
    /// <param name="fromItem">The from item name.</param>
    /// <param name="to">The to entity.</param>
    /// <param name="toItem">The to item name.</param>
    /// <param name="modified">Set <b>true</b> if modified.</param>
    /// <param name="byDefault">The default value.</param>
    /// <returns><b>true</b> if modified, otherwise <b>false</b>.</returns>
    public static bool SetDoubleItem(XEntity from, string fromItem, XEntity to, string toItem, double byDefault = 0.0)
    {
        Debug.Assert(!string.IsNullOrEmpty(fromItem) && !string.IsNullOrEmpty(toItem));
        var data = from.GetItemText(fromItem).Trim();
        var text = to.GetItemText(toItem).Trim();
        if (data.Length > 0 && !data.Equals(text))
        {
            double result, value;
            var cultures = new CultureInfo[] { CultureInfo.InvariantCulture, CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture, CultureInfo.InstalledUICulture };
            foreach (var culture in cultures)
            {
                if (double.TryParse(data, NumberStyles.Number, culture, out result))
                {
                    if (text.Length > 0)
                    {
                        foreach (var cltr in cultures)
                        {
                            if (double.TryParse(text, NumberStyles.Number, cltr, out value) && result == value)
                                return false;
                        }
                    }
                    to[toItem] = result;
                    return true;
                }
            }
        }
        if (text.Length == 0 && byDefault != 0.0)
        {
            to[toItem] = byDefault;
            return true;
        }
        return false;
    }
    /// <summary>
    /// Merge date & time item.
    /// </summary>
    /// <param name="from">The from entity.</param>
    /// <param name="fromItem">The from item name.</param>
    /// <param name="to">The to entity.</param>
    /// <param name="toItem">The to item name.</param>
    /// <param name="today">Use default today value.</param>
    /// <returns><b>true</b> if modified, otherwise <b>false</b>.</returns>
    public static bool SetDateTimeItem(XEntity from, string fromItem, XEntity to, string toItem, bool today = false)
    {
        Debug.Assert(!string.IsNullOrEmpty(fromItem) && !string.IsNullOrEmpty(toItem));
        DateTime dt;
        if (DateTime.TryParse(from.GetItemText(fromItem), out dt))
        {
            if (to.GetItemText(toItem).Length > 0)
            {
                var exist = to.GetItemValue<DateTime>(toItem);
                if (dt != exist && Math.Abs(dt.Subtract(exist).TotalHours) >= 5)
                {
                    to[toItem] = dt;
                    return true;
                }
            }
            else if (dt != DateTime.MinValue)
            {
                to[toItem] = dt;
                return true;
            }
        }
        if (today)
        {
            var exist = to.GetItemValue<DateTime>(toItem);
            if (Math.Abs(DateTime.Today.Subtract(exist).TotalHours) >= 5)
            {
                to[toItem] = DateTime.Today;
                return true;
            }
        }
        return false;
    }
    /// <summary>
    /// Merge boolean item.
    /// </summary>
    /// <param name="from">The from entity.</param>
    /// <param name="fromItem">The from item name.</param>
    /// <param name="to">The to entity.</param>
    /// <param name="toItem">The to item name.</param>
    /// <param name="byDefault">The default value.</param>
    /// <returns><b>true</b> if modified, otherwise <b>false</b>.</returns>
    public static bool SetBooleanItem(XEntity from, string fromItem, XEntity to, string toItem, bool byDefault = false)
    {
        Debug.Assert(!string.IsNullOrEmpty(fromItem) && !string.IsNullOrEmpty(toItem));
        var data = false;
        var useDefault = false;
        var s = from.GetItemText(fromItem).ToLower().Trim();
        switch (s)
        {
            case "":
                useDefault = true;
                break;
            case "0":
            case "no":
            case "нет":
            case "ложь":
                data = false;
                break;
            case "1":
            case "yes":
            case "да":
            case "истина":
                data = true;
                break;
            default:
                data = from.GetItemValue<bool>(fromItem);
                break;
        }
        if (to.GetItemText(toItem).Length > 0)
        {
            var exist = to.GetItemValue<bool>(toItem);
            if (data != exist)
            {
                to[toItem] = data;
                return true;
            }
        }
        else
        {
            to[toItem] = useDefault ? byDefault : data;
            return true;
        }
        return false;
    }

    internal static bool TryParse(string text, string entityName, out XEntity entity)
    {
        // sanity
        entity = null;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        // sanity first
        if (text[0] != '{') return false;

        // sanity last
        int last = Pair(text, 0);
        if (last < 1) return false;
        Debug.Assert(last == text.Length - 1);

        // entity name
        entity = new XEntity(entityName);

        // for each element
        int idx = 1;
        while (true)
        {
            // field pair?
            int n = text.IndexOf(':', idx);
            if (n < idx) break;

            // for next key
            while (idx < text.Length && text[idx] != '"')
            {
                Debug.Assert(text[idx] == ',' || Char.IsWhiteSpace(text, idx));
                idx++;
            }

            // key
            var key = RemoveQuotes(text.Substring(idx, n - idx).Trim());
            Debug.Assert(!string.IsNullOrWhiteSpace(key));
            if (string.IsNullOrWhiteSpace(key)) break;

            // value
            int pair;
            string txt;
            int start = n + 1;
            for (int i = n + 1; i <= last; i++)
            {
                if (start == i)
                {
                    if (Char.IsWhiteSpace(text, i))
                    {
                        start = i;
                        continue;
                    }
                    if (text[i] == '"')
                    {
                        // text value?
                        pair = Next(text, i);
                        Debug.Assert(pair > i);
                        if (pair <= i) return false;
                        txt = RemoveQuotes(text.Substring(i, pair - i + 1));
                        entity.Items.Add(key, txt.Trim(' ', '\t', '\r', '\n'));
                        idx = pair + 1;
                        break;
                    }
                    else if (text[i] == '{')
                    {
                        // object value?
                        pair = Pair(text, i);
                        Debug.Assert(pair > i);
                        if (pair <= i) return false;
                        var body = text.Substring(i, pair - i + 1);
                        if (body.Length > 0 && body[1] == '#')
                        {
                            XEntityCollection collection;
                            if (XEntityCollection.TryParse(body, out collection))
                            {
                                entity.Items.Add(key, collection);
                            }
                            else
                            {
                                Debug.Assert(false, "strange child entity collection!");
                                return false;
                            }
                        }
                        else
                        {
                            XEntity child;
                            if (TryParse(body, key, out child))
                            {
                                entity.Items.Add(key, child);
                                idx = pair + 1;
                            }
                            else
                            {
                                Debug.Assert(false, "strange child entity!");
                                return false;
                            }
                        }
                        idx = pair + 1;
                        break;
                    }
                    else if (text[i] == '[')
                    {
                        // array value?
                        pair = Pair(text, i);
                        Debug.Assert(pair > i);
                        if (pair <= i) return false;
                        XEntityCollection collection;
                        if (XEntityCollection.TryParse(text.Substring(i, pair - i + 1), out collection))
                        {
                            entity.Items.Add(key, collection);
                            idx = pair + 1;
                        }
                        else
                        {
                            Debug.Assert(false, "strange child entity collection!");
                            return false;
                        }
                        break;
                    }
                    else
                    {
                        txt = TextForValue(text, i);
                        if (!string.IsNullOrEmpty(txt))
                        {
                            entity.Items.Add(key, GetValue(txt));
                            idx = i + txt.Length;
                            break;
                        }
                    }
                }
            }
        }

        // done
        return true;
    }

    public static string TextForValue(string text, int index)
    {
        int length = 0;
        var number = false;
        for (int i = index; i < text.Length; i++)
        {
            if (Char.IsWhiteSpace(text, i)) break;
            if (Char.IsPunctuation(text, i))
            {
                if (!number || text[i] == ',') break;
            }
            if (Char.IsNumber(text, i))
            {
                number |= (i == index);
            }
            length++;
        }
        return text.Substring(index, length);
    }
    public static object GetValue(string text)
    {
        // is integer?
        int n;
        if (int.TryParse(text, out n))
        {
            return n;
        }

        // is long integer?
        long l;
        if (long.TryParse(text, out l))
        {
            return l;
        }

        // is GUID?
        Guid guid;
        if (Guid.TryParse(text, out guid))
        {
            return guid;
        }

        // is boolean?
        bool b;
        if (bool.TryParse(text, out b) && text.Equals(new XElement("n", b).Value))
        {
            return b;
        }

        // set of format providers
        var culture = CultureInfo.InvariantCulture;

        // is numeric?
        double d;
        if (XProxy.IsNumber(text))
        {
            if (double.TryParse(text, NumberStyles.Float, culture, out d) && text.Equals(new XElement("n", d).Value))
                return d;
            if (double.TryParse(text, NumberStyles.Any, culture, out d) && text.Equals(new XElement("n", d).Value))
                return d;
        }

        // is date & time?
        if (XProxy.IsUtcDate(text))
        {
            var parts = text.Split('T');
            var ss = parts[0].Split('-');
            if (ss.Length >= 3 && XProxy.IsNumber(ss[0]) && XProxy.IsNumber(ss[1]) && XProxy.IsNumber(ss[2]))
            {
                int year = int.Parse(ss[0]);
                int month = int.Parse(ss[1]);
                int day = int.Parse(ss[2]);

                int hour = 0, minute = 0, second = 0;
                if (parts.Length > 1)
                {
                    ss = parts[1].Split(':', '+');
                    if (XProxy.IsNumber(ss[0])) hour = int.Parse(ss[0]);
                    if (ss.Length > 1 && XProxy.IsNumber(ss[1])) minute = int.Parse(ss[1].Trim(' ', '$'));
                    if (ss.Length > 2 && XProxy.IsNumber(ss[2])) second = int.Parse(ss[2].Trim(' ', '$'));
                }

                return new DateTime(year, month, day, hour, minute, second);
            }
        }

        // done
        Debug.Assert(false, "strange value!");
        return text;
    }

    internal static string ToText(XEntity entity)
    {
        // initialization
        var sb = new StringBuilder();

        // begin
        sb.Append('{'); //.Append(entity.EntityName).Append(":{");

        // body
        for (int i = 0; i < entity.Items.Count; i++)
        {
            // separator
            if (i > 0) sb.Append(',');
            var item = entity.Items[i];

            // empty value?
            var value = item.Value;
            if (value is Guid && (Guid)value == Guid.Empty) continue;
            if (value.ToString().Length == 0) continue;
            if (IsZero(value)) continue;

            // pair
            sb.Append(AddQuotes(item.Key)).Append(':').Append(ToText(value, true));
        }

        // end
        sb.Append('}');

        // done
        return sb.ToString();
    }
    public static string ToText(object value, bool quotes = false)
    {
        // sanity
        if (value == null) return string.Empty;

        // text?
        if (value is string)
        {
            if (quotes)
                return AddQuotes((string)value);
            return (string)value;
        }

        // guid?
        if (value is Guid)
        {
            return ((Guid)value).ToString("D");
        }

        // date & time?
        if (value is DateTime)
        {
            return ((DateTime)value).ToString("s");
        }

        // boolean?
        if (value is bool)
        {
            return ((bool)value) ? "true" : "false";
        }

        // integer?
        if (value is byte || value is int || value is short || value is long)
        {
            return value.ToString();
        }

        // number?
        if (value is double || value is float || value is decimal)
        {
            return Convert.ToDouble(value).ToString("F");
        }

        // entity part?
        if (value is XOptionSetValue || value is IEntityReference || value is XEntityCollection)
        {
            return value.ToString();
        }

        // entity?
        if (value is XEntity)
        {
            return XEntity.ToText((XEntity)value);
        }

        // unknown?
        Debug.Assert(false, "strange value type!");
        return value.ToString();
    }

    static bool IsZero(object value)
    {
        if (value is Int16 || value is Int32 || value is Int64 || value is UInt16 || value is UInt32 || value is UInt64)
        {
            return Convert.ToInt64(value) == 0;
        }
        return false;
    }
    internal static string AddQuotes(string text)
    {
        return string.Format("\"{0}\"", text.Replace("\"", "\"\""));
    }
    internal static string RemoveQuotes(string text)
    {
        Debug.Assert(text.StartsWith("\"") && text.EndsWith("\""));
        return text.Substring(1, text.Length - 2).Replace("\"\"", "\"");
    }
    static int Next(string text, int index)
    {
        // sanity
        Debug.Assert(!string.IsNullOrEmpty(text) && index >= 0 && index < text.Length);
        if (string.IsNullOrEmpty(text) || index < 0 || index >= text.Length) return -1;

        // search
        int cnt = 1;
        var ch = text[index];
        for (int i = index + 1; i < text.Length; i++)
        {
            if (ch == text[i])
            {
                // found?
                cnt++;
                if (cnt % 2 != 0) continue;
                if (i + 1 < text.Length && ch == text[i + 1]) continue;

                // done
                return i;
            }
        }

        // not found
        return -1;
    }
    internal static int Pair(string text, int index)
    {
        // sanity
        Debug.Assert(!string.IsNullOrEmpty(text) && index >= 0 && index < text.Length);
        if (string.IsNullOrEmpty(text) || index < 0 || index >= text.Length) return -1;

        // pair of symbol
        var ch = text[index];
        var pair = ' ';
        switch (ch)
        {
            case '{': pair = '}'; break;
            case '[': pair = ']'; break;
            case '(': pair = ')'; break;
            case '«': pair = '»'; break;
        }
        Debug.Assert(pair != ' ');
        if (pair == ' ') return -1;

        // search
        int cnt = 0;
        for (int i = index + 1; i < text.Length; i++)
        {
            if (pair == text[i])
            {
                if (cnt == 0)
                {
                    // found
                    return i;
                }
                cnt--;
            }
            else if (ch == text[i])
            {
                cnt++;
            }
        }

        // not found
        return -1;
    }

    #endregion
}

// -------------------------------------------------------------------------
#region ** entity collections

/// <summary>Contains a collection of entity items (attributes).</summary>
public partial class XItemCollection : ObservableCollection<KeyValuePair<string, object>>
{
}
/// <summary>Contains a collection of entity formatted values.</summary>
public partial class XFormattedValueCollection : ObservableCollection<KeyValuePair<string, string>>
{
}
/// <summary>Contains a paginated collection of entities.</summary>
public partial class XEntityCollection : XCollection<XEntity, string>, IEntitySet
{
    public XEntityCollection()
    {
        EntityName = string.Empty;
    }

    /// <summary>Gets logical name of entity.</summary>
    public string EntityName { get; internal set; }

    /// <summary>Gets deleted entities of this collection.</summary>
    //public XEntityCollection Deleted { get; private set; }

    /// <summary>
    /// Gets or sets a paging cookie of the request.
    /// </summary>
    public string PagingCookie { get; set; }

    // ** IEntitySet

    /// <summary>Creating entities.</summary>
    public ICollection<XEntity> Creating
    {
        get
        {
            var list = new Collection<XEntity>();
            foreach (var entity in this)
            {
                if (entity.State == XEntityState.Created)
                    list.Add(entity);
            }
            return list;
        }
    }
    /// <summary>Updating entities.</summary>
    public ICollection<XEntity> Updating
    {
        get
        {
            var list = new Collection<XEntity>();
            foreach (var entity in this)
            {
                if (entity.State == XEntityState.Modified)
                    list.Add(entity);
            }
            return list;
        }
    }

    /// <summary>Gets more records flag.</summary>
    public bool HasMore { get; internal set; }
    /// <summary>Gets total count of entities in the server.</summary>
    public long TotalCount { get; set; }

    /// <summary>
    /// Clone collection.
    /// </summary>
    /// <returns>THe new cloned collection.</returns>
    public XEntityCollection Clone()
    {
        var clone = (XEntityCollection)MemberwiseClone();
        return clone;
    }

    // ** overrides

    /// <summary>
    /// Override insert.
    /// </summary>
    /// <param name="index">The item index.</param>
    /// <param name="item">The inserting entity.</param>
    protected override void InsertItem(int index, XEntity item)
    {
        // insert entity
        base.InsertItem(index, item);
        
        // collection name
        if (string.IsNullOrEmpty(EntityName))
        {
            EntityName = item.EntityName;
        }
    }

    /// <summary>
    /// Override remove.
    /// </summary>
    /// <param name="index">The item index.</param>
    protected override void RemoveItem(int index)
    {
        var entity = this[index];
        base.RemoveItem(index);
        if (entity.State == XEntityState.Created)
        {
            Deleting.Remove(entity);
        }
    }

    internal void Sort(string primaryKey, string parentKey)
    {
        // sanity
        if (string.IsNullOrEmpty(primaryKey) || string.IsNullOrEmpty(parentKey))
        {
            return;
        }

        // parent sort
        int idx = 0;
        while (idx < Count - 1)
        {
            var parent = this[idx][parentKey];
            if (parent != null)
            {
                string parentId;
                if (parent is XEntityReference)
                {
                    if (!((XEntityReference)parent).EntityName.Equals(EntityName))
                    {
                        // sanity entity
                        idx++;
                        continue;
                    }
                    parentId = ((XEntityReference)parent).Id;
                }
                else
                {
                    parentId = parent.ToString();
                }
                if (!string.IsNullOrEmpty(parentId))
                {
                    for (int i = idx + 1; i < Count; i++)
                    {
                        var obj = this[i][primaryKey];
                        var id = (obj is XEntityReference) ? ((XEntityReference)obj).Id : obj.ToString();
                        if (id.Equals(parentId))
                        {
                            // swap
                            var swap = this[idx];
                            this[idx] = this[i];
                            this[i] = swap;
                            idx--;
                            break;
                        }
                    }
                }
            }
            idx++;
        }
    }
    //static int GetRefCount(XEntity entity, List<XColumnSchema> columns)
    //{
    //    int count = 0;
    //    foreach (var column in columns)
    //    {
    //        var reference = string.Empty;
    //        if (entity.Items.ContainsKey(column.ColumnName))
    //            reference = entity[column.ColumnName].ToString();
    //        if (entity.Items.ContainsKey(column.ColumnName.ToLower()))
    //            reference = entity[column.ColumnName.ToLower()].ToString();
    //        if (!string.IsNullOrEmpty(reference) && reference.StartsWith("#REF="))
    //            count++;
    //    }
    //    return count;
    //}

    /// <summary>
    /// Try parse string value from ToString() method.
    /// </summary>
    /// <param name="text">The string value.</param>
    /// <param name="collection">The new <see cref="XEntityCollection"/> object>, otherwise <b>null</b>.</param>
    /// <returns>Success flag.</returns>
    public static bool TryParse(string text, out XEntityCollection collection)
    {
        // sanity
        collection = null;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        // sanity first
        if (text[0] != '{') return false;

        // sanity last
        int last = XEntity.Pair(text, 0);
        if (last < 1) return false;
        Debug.Assert(last == text.Length - 1);

        // search
        int nameIdx = text.IndexOf("{#");
        int entityIdx = text.IndexOf("[", nameIdx + 1);

        // sanity
        if (nameIdx != 0 || entityIdx < 0)
        {
            Debug.Assert(false, "strange collection!");
            return false;
        }

        // body
        //var body = text.Substring(entityIdx);
        //Debug.Assert(body.EndsWith("]}"));
        //body = body.Substring(1, body.Length - 3);

        // entity name
        var entityName = text.Substring(nameIdx + 2, entityIdx - 2).Trim();
        collection = new XEntityCollection();
        collection.EntityName = entityName;

        // for each element
        int idx = entityIdx + 1;
        while (idx < last)
        {
            int n = XEntity.Pair(text, idx);
            if (n < idx) break;
            int length = n - idx + 1;
            var s = text.Substring(idx, length);
            XEntity entity;
            if (XEntity.TryParse(s, entityName, out entity))
            {
                collection.Add(entity);
                idx += length;
            }
            else
            {
                Console.WriteLine("WARING! Strange entity text: {0}", s);
            }
            idx++;
        }

        // done
        return true;
    }

    /// <summary>
    /// Returns a string that represents the current object.
    /// </summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
    {
        // initialization
        var sb = new StringBuilder();

        // begin
        sb.Append("{#").Append(EntityName).Append('[');

        // body
        for (int i = 0; i < this.Count; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append(XEntity.ToText(this[i]));
        }

        // end
        sb.Append("]}");

        // done
        return sb.ToString();
    }
}

#endregion

// -------------------------------------------------------------------------
#region ** entity relationship and collection

/// <summary>The entity relationship.</summary>
public partial class XRelationship
{
    /// <summary>
    /// Initializes entity reference with setting the logical name and entity ID.
    /// </summary>
    /// <param name="schemaName">The schema name.</param>
    /// <param name="related">The related flag.</param>
    public XRelationship(string schemaName, bool? related)
    {
        SchemaName = schemaName;
        Related = related;
    }

    /// <summary>Entity related or no.</summary>
    public bool? Related { get; protected set; }
    /// <summary>The relationship schema name.</summary>
    public string SchemaName { get; protected set; }
}

/// <summary>Contains a collection of entity relationships.</summary>
public partial class XRelatedEntityCollection : ObservableCollection<KeyValuePair<XRelationship, XEntityCollection>>
{
}

#endregion

// -------------------------------------------------------------------------
#region ** entity reference and collection

/// <summary>The entity reference.</summary>
[Serializable]
public partial class XEntityReference : IEntityReference
{
    /// <summary>
    /// Default initialization.
    /// </summary>
    public XEntityReference()
    {
    }
    /// <summary>
    /// Initializes entity reference with setting the logical name and entity ID.
    /// </summary>
    /// <param name="entityName">The logical name.</param>
    /// <param name="id">The entity ID.</param>
    public XEntityReference(string entityName, string id)
    {
        EntityName = entityName;
        Id = id;
    }

    /// <summary>
    /// Gets or sets identifier of record.
    /// </summary>
    public string Id { get; set; }
    /// <summary>
    /// Gets or sets logical entity name.
    /// </summary>
    public string EntityName { get; set; }
    /// <summary>
    /// Gets or sets the primary value of the entity.
    /// </summary>
    public string Text { get; set; }

    /// <summary>
    /// Parse string value from ToString() method.
    /// </summary>
    /// <param name="text">The string value.</param>
    /// <returns>Create new <see cref="XEntityReference"/> object or <see cref="ArgumentOutOfRangeException"/>.</returns>
    public static XEntityReference Parse(string text)
    {
        XEntityReference entityRef;
        if (TryParse(text, out entityRef))
        {
            throw new ArgumentOutOfRangeException("text");
        }
        return entityRef;
    }
    /// <summary>
    /// Try parse string value from ToString() method.
    /// </summary>
    /// <param name="text">The string value.</param>
    /// <param name="entityRef">The new <see cref="XEntityReference"/> object>, otherwise <b>null</b>.</param>
    /// <returns>Success flag.</returns>
    public static bool TryParse(string text, out XEntityReference entityRef)
    {
        // sanity
        entityRef = null;
        if (string.IsNullOrEmpty(text) || text.Length > 1024)
        {
            return false;
        }

        // sanity first
        if (text[0] != '#') return false;

        // search
        var s = text.ToUpper();
        int refIdx = s.IndexOf("#REF=");
        int txtIdx = s.IndexOf("/#TXT=");
        int idIdx = s.IndexOf("/#ID=");

        // sanity
        if (refIdx != 0 || idIdx < refIdx) return false;
        if (txtIdx < 0) txtIdx = idIdx;

        // parse
        entityRef = new XEntityReference();
        entityRef.EntityName = text.Substring(5, txtIdx - 5).Trim();
        if (txtIdx > 0 && txtIdx < idIdx)
            entityRef.Text = text.Substring(txtIdx + 6, idIdx - txtIdx - 6).Trim();
        entityRef.Id = text.Substring(idIdx + 5).Trim();

        // done
        return true;
    }

    /// <summary>
    /// Serves as a hash function for a particular type.
    /// </summary>
    /// <returns>A hash code for the current object.</returns>
    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

    /// <summary>
    /// Determines whether the specified <see cref="object"/> is equal to the current <see cref="object"/>.
    /// </summary>
    /// <param name="obj">The object to compare with the current object.</param>
    /// <returns><b>true</b> if the specified <see cref="object"/> is equal to the current <see cref="object"/>, otherwise, <b>false</b>.</returns>
    public override bool Equals(object obj)
    {
        if (obj != null && obj is XEntityReference)
        {
            var other = (XEntityReference)obj;
            return EntityName.Equals(other.EntityName) && Id.Equals(other.Id);
        }
        return base.Equals(obj);
    }

    /// <summary>
    /// Returns a string that represents the current object.
    /// </summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
    {
        // initialization
        var id = string.IsNullOrEmpty(Id) ? string.Empty : Id;
        var text = string.IsNullOrWhiteSpace(Text) ? string.Empty : Text;
        var name = string.IsNullOrWhiteSpace(EntityName) ? string.Empty : EntityName;

        // short done
        if (string.IsNullOrEmpty(Text))
            return string.Format(@"#REF={0}/#ID={1}", name, id);

        // done
        return string.Format(@"#REF={0}/#TXT={1}/#ID={2}", name, text, id);
    }
}

/// <summary>
/// Contains a collection of entity references.
/// </summary>
public partial class XEntityReferenceCollection : ObservableCollection<XEntityReference>
{
}

#endregion

// -------------------------------------------------------------------------
#region ** query expression enumerations

/// <summary>
/// The order enumeration type specifies.
/// </summary>
public enum XOrderType : byte
{
    /// <summary>
    /// Specifies that the values of the specified attribute should be sorted in
    /// ascending order, from lowest to highest.
    /// </summary>
    Ascending = 0,
    /// <summary>
    /// Specifies that the values of the specified attribute should be sorted in
    /// descending order, from highest to lowest.
    /// </summary>
    Descending = 1,
}
/// <summary>
/// The logical enumeration type specifies.
/// </summary>
public enum XLogicalOperator : byte
{
    /// <summary>Specifies that a logical AND operation is performed.</summary>
    And = 0,
    /// <summary>Specifies that a logical OR operation is performed.</summary>
    Or = 1,
}
/// <summary>
/// The join enumeration type specifies.
/// </summary>
public enum XJoinOperator : byte
{
    /// <summary>
    /// Specifies that the values in the attributes being joined are compared using
    /// a comparison operator.
    /// </summary>
    Inner = 0,
    /// <summary>
    /// Specifies that all instances of the entity in the FROM clause are returned
    /// if they meet WHERE or HAVING search conditions.
    /// </summary>
    LeftOuter = 1,
    /// <summary>
    /// Specifies that only one value of the two joined attributes is returned if
    /// an equal-join operation is performed and the two values are identical.
    /// </summary>
    Natural = 2,
}
/// <summary>
/// The ConditionOperator enumeration type specifies the possible values for the condition operator in a condition expression.
/// </summary>
public enum XConditionOperator : byte
{
    /// <summary>Specifies that two expressions are compared for equality.</summary>
    Equal = 0,
    /// <summary>Specifies that two expressions are compared for inequality.</summary>
    NotEqual = 1,
    /// <summary>Specifies that two expressions are compared for the greater than condition.</summary>
    GreaterThan = 2,
    /// <summary>Specifies that two expressions are compared for a less than condition.</summary>
    LessThan = 3,
    /// <summary>Specifies that two expressions are compared for greater than or equal to conditions.</summary>
    GreaterEqual = 4,
    /// <summary>Specifies that two expressions are compared for greater than or equal to conditions.</summary>
    LessEqual = 5,
    /// <summary>Specifies that the character string is matched to a specified pattern.</summary>
    Like = 6,
    /// <summary>Specifies that the character string is matched to a specified pattern.</summary>
    NotLike = 7,
    /*
    /// <summary>Specifies that a given value is matched to a value in a list.</summary>
    In = 8,
    /// <summary>Specifies that a given value is not matched to a value in a subquery or a list.</summary>
    NotIn = 9,
    /// <summary>Specifies that the value is between two values.</summary>
    Between = 10,
    /// <summary>Specifies that the value is not between two values.</summary>
    NotBetween = 11,
    */
    /// <summary>Specifies that the value is null.</summary>
    Null = 12,
    /// <summary>Specifies that the value is not null.</summary>
    NotNull = 13,
}

#endregion

// -------------------------------------------------------------------------
#region ** query expression classes

/// <summary>
/// Contains a condition expression used to filter the results of the query.
/// </summary>
public partial class XCondition
{
    /// <summary>
    /// Initialization condition setting the item (attribute) name, operator and a collection of values.
    /// </summary>
    /// <param name="itemName">The logical name of the item (attribute).</param>
    /// <param name="conditionOperator">The condition operator.</param>
    /// <param name="values">The value of item (attribute).</param>
    public XCondition(string itemName, XConditionOperator conditionOperator, object value)
    {
        ItemName = itemName;
        Operator = conditionOperator;
        Value = value;
    }
    /// <summary>
    /// Gets or sets the logical name of the item (attribute) in the condition expression.
    /// </summary>
    public string ItemName { get; set; }
    /// <summary>
    /// Gets or sets the condition operator.
    /// </summary>
    public XConditionOperator Operator { get; set; }
    /// <summary>
    /// Gets or sets the value for the item (attribute).
    /// </summary>
    public object Value { get; private set; }
}
/// <summary>
/// Specifies complex condition and logical filter expressions used for filtering
/// the results of the query.
/// </summary>
public partial class XFilter
{
    /// <summary>
    /// Default initialization with AND filter operator.
    /// </summary>
    public XFilter()
        : this(XLogicalOperator.And)
    {
    }
    /// <summary>
    /// Initialization with filter operator.
    /// </summary>
    /// <param name="filterOperator">The logical AND/OR filter operators.</param>
    public XFilter(XLogicalOperator filterOperator)
    {
        FilterOperator = filterOperator;
        Conditions = new ObservableCollection<XCondition>();
        Filters = new ObservableCollection<XFilter>();
    }
    /// <summary>
    /// Gets condition expressions that include attributes, condition operators,
    /// and attribute values.
    /// </summary>
    public ObservableCollection<XCondition> Conditions { get; private set; }
    /// <summary>Gets or sets logical AND/OR filter operators.</summary>
    public XLogicalOperator FilterOperator { get; set; }
    /// <summary>
    /// Gets a hierarchy of condition and logical filter expressions that filter
    /// the results of the query.
    /// </summary>
    public ObservableCollection<XFilter> Filters { get; private set; }
    /// <summary>
    /// Adds a condition to the filter expression setting the condition expression.
    /// </summary>
    /// <param name="condition">The condition expression.</param>
    public void AddCondition(XCondition condition)
    {
        Conditions.Add(condition);
    }
    /// <summary>
    /// Adds a condition to the filter expression setting the attribute name, condition
    /// operator, and value array.
    /// </summary>
    /// <param name="itemName">The name of the item (attribute).</param>
    /// <param name="conditionOperator">The condition operator.</param>
    /// <param name="value">The value of the item (attribute).</param>
    public void AddCondition(string itemName, XConditionOperator conditionOperator, object value)
    {
        Conditions.Add(new XCondition(itemName, conditionOperator, value));
    }
    /// <summary>
    /// Adds a child filter to the filter expression.
    /// </summary>
    /// <param name="childFilter">The child filter expression.</param>
    public void AddFilter(XFilter childFilter)
    {
        Filters.Add(childFilter);
    }
    /// <summary>
    /// Adds a child filter to the filter expression setting the logical operator.
    /// </summary>
    /// <param name="filterOperator">The filter logical operator.</param>
    /// <returns>The child filter expression.</returns>
    public XFilter AddFilter(XLogicalOperator filterOperator)
    {
        var childFilter = new XFilter(filterOperator);
        Filters.Add(childFilter);
        return childFilter;
    }
}
/// <summary>
/// Sets the order in which the entity instances are returned from the query.
/// </summary>
public partial class XOrder
{
    /// <summary>
    /// Default initialization.
    /// </summary>
    public XOrder() { }
    /// <summary>
    /// Initializations order expression setting the item (attribute) name and the order type.
    /// </summary>
    /// <param name="itemName">The name of the item (attribute).</param>
    /// <param name="orderType">The order type.</param>
    public XOrder(string itemName, XOrderType orderType)
    {
        ItemName = itemName;
        OrderType = orderType;
    }
    /// <summary>Gets or sets the name of the attribute in the order expression.</summary>
    public string ItemName { get; set; }
    /// <summary>Gets or sets the ascending or descending order.</summary>
    public XOrderType OrderType { get; set; }
}
/// <summary>
/// Specifies the links between multiple entity types used in creating complex queries.
/// </summary>
/// <remarks>Warning! Not supported links between multiple entity.</remarks>
public partial class XLink
{
    /// <summary>
    /// Default initialization.
    /// </summary>
    public XLink()
    {
        Columns = new ObservableCollection<string>();
    }
    /// <summary>
    /// Initialization link with setting the required properties.
    /// </summary>
    /// <param name="fromEntityName">The logical name of the entity to link from.</param>
    /// <param name="toEntityName">The logical name of the entity to link to.</param>
    /// <param name="fromItemName">The name of the item (attribute) to link from.</param>
    /// <param name="toItemName">The name of the item (attribute) to link to.</param>
    /// <param name="joinOperator">The join operator.</param>
    public XLink(string fromEntityName, string toEntityName, string fromItemName, string toItemName, XJoinOperator joinOperator)
        : this()
    {
        FromEntityName = fromEntityName;
        ToEntityName = toEntityName;
        FromItemName = fromItemName;
        ToItemName = toItemName;
        JoinOperator = joinOperator;
    }

    /// <summary>
    /// Gets set of columns (items, attributes). If one column with empty name then all colimns.
    /// </summary>
    public ObservableCollection<string> Columns { get; private set; }
    /// <summary>
    /// Gets or sets an alias for the entity.
    /// </summary>
    public string Alias { get; set; }

    /// <summary>
    /// Gets or sets the join operator.
    /// </summary>
    public XJoinOperator JoinOperator { get; set; }
    /// <summary>
    /// Gets or sets the complex condition and logical filter expressions that filter the results of the query.
    /// </summary>
    public XFilter Criteria { get; set; }
    /// <summary>
    /// Gets or sets the logical name of the item (attribute) of the entity that you are linking from.
    /// </summary>
    public string FromItemName { get; set; }
    /// <summary>
    /// Gets or sets the logical name of the entity that you are linking from.
    /// </summary>
    public string FromEntityName { get; set; }
    /// <summary>
    /// Gets or sets the logical name of the item (attribute) of the entity that you are linking to.
    /// </summary>
    public string ToItemName { get; set; }
    /// <summary>
    /// Gets or sets the logical name of the entity that you are linking to.
    /// </summary>
    public string ToEntityName { get; set; }
}
/// <summary>
/// Specifies a number of pages and a number of entity instances per page to return from the query.
/// </summary>
public partial class XPagingInfo
{
    /// <summary>
    /// Gets or sets the number of entity instances returned per page.
    /// </summary>
    public int Count { get; set; }
    /// <summary>
    /// Gets or sets the number of pages returned from the query.
    /// </summary>
    public int PageNumber { get; set; }
    /// <summary>
    /// Gets or sets a paging cookie of the request.
    /// </summary>
    public string PagingCookie { get; set; }
    /// <summary>
    /// Sets whether the total number of records should be returned from the query.
    /// </summary>
    public bool ReturnTotalCount { get; set; }
}

#endregion

/// <summary>
///  Retrieves instances of a specific entity type by using a complex query.
/// </summary>
/// <remarks>Warning! Not supported links between multiple entity.</remarks>
public partial class XQuery
{
    // -------------------------------------------------------------------------
    #region ** object model

    /// <summary>
    /// Default initialization.
    /// </summary>
    public XQuery()
    {
        Columns = new ObservableCollection<string>();
        Orders = new ObservableCollection<XOrder>();
        Links = new ObservableCollection<XLink>();
    }
    /// <summary>
    /// Initialization query expression setting the logical name of the entity.
    /// </summary>
    /// <param name="entityName">The logical name of the entity.</param>
    public XQuery(string entityName)
        : this()
    {
        EntityName = entityName;
    }

    /// <summary>
    /// Gets include set of columns (items, attributes). If one column with empty name then all colimns.
    /// </summary>
    public ObservableCollection<string> Columns { get; private set; }
    /// <summary>
    /// Gets or sets the complex condition and logical filter expressions that filter the results of the query.
    /// </summary>
    public XFilter Criteria { get; set; }
    /// <summary>
    /// Gets or sets whether the results of the query contain duplicate entity instances.
    /// </summary>
    public bool Distinct { get; set; }
    /// <summary>
    /// Gets or sets the logical name of the entity.
    /// </summary>
    public string EntityName { get; set; }
    /// <summary>
    /// Gets or sets a value that indicates that no shared locks are issued against
    /// the data that would prohibit other transactions from modifying the data in
    /// the records returned from the query.
    /// </summary>
    public bool NoLock { get; set; }
    /// <summary>
    /// Gets the orders in which the entity instances are returned from the query.
    /// </summary>
    public ObservableCollection<XOrder> Orders { get; private set; }
    /// <summary>
    /// Gets the specified links to the query expression setting.
    /// </summary>
    public ObservableCollection<XLink> Links { get; private set; }
    /// <summary>
    /// Gets or sets the number of pages and the number of entity instances per page returned from the query.
    /// </summary>
    public XPagingInfo PageInfo { get; set; }

    /// <summary>Gets or sets top rows to the query expression setting.</summary>
    public long Top { get; set; }
    /// <summary>Gets or sets skip rows to the query expression setting.</summary>
    public long Skip { get; set; }

    /// <summary>Gets or sets extension data (optional).</summary>
    public object Tag { get; set; }

    /// <summary>
    /// Adds the specified link to the query expression setting the entity name to link to,
    /// the item (attribute) name to link from and the item (attribute) name to link to.
    /// </summary>
    /// <param name="toEntityName">The name of entity to link from.</param>
    /// <param name="fromItemName">The name of the item (attribute) to link from.</param>
    /// <param name="toItemName">The name of the item (attribute) to link to.</param>
    /// <param name="joinOperator">The join operator.</param>
    /// <returns>The added link expression.</returns>
    public XLink AddLink(string toEntityName, string fromItemName, string toItemName, XJoinOperator joinOperator = XJoinOperator.Inner)
    {
        var link = new XLink(EntityName, toEntityName, fromItemName, toItemName, joinOperator);
        Links.Add(link);
        return link;
    }
    /// <summary>
    /// Adds the specified order expression to the query expression.
    /// </summary>
    /// <param name="itemName">The name of the item (attribute).</param>
    /// <param name="orderType">The order type.</param>
    public void AddOrder(string itemName, XOrderType orderType)
    {
        Orders.Add(new XOrder(itemName, orderType));
    }

    #endregion

    // -------------------------------------------------------------------------
    #region ** static object model

    /// <summary>
    /// Convert value to OData text.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The text value.</returns>
    public static string ToText(object value)
    {
        // text?
        if (value is string s)
        {
            return XEntity.AddQuotes(s);
        }

        // GUID?
        if (value is Guid guid)
        {
            return guid.ToString("D");
        }

        // date & time?
        if (value is DateTime dt)
        {
            return dt.ToString("s");
        }

        // boolean?
        if (value is bool b)
        {
            return b ? "true" : "false";
        }

        // integer?
        if (value is byte || value is int || value is short || value is long)
        {
            return value.ToString();
        }

        // number?
        if (value is double || value is float || value is decimal)
        {
            return Convert.ToDouble(value).ToString("F");
        }

        // null value
        return "null";
    }

    public static XQuery FromOData(ISchema schema, string table, string query)
    {
        var collection = schema.Tables as XCollection<ITableSchema, string>;
        var tableSchema = collection.GetBy(table);
        if (tableSchema != null)
        {
            query = query.Trim();
            var q = new XQuery(table);
            if (!string.IsNullOrWhiteSpace(query))
            {
                Dictionary<string, string> queryOptions = new();
                var splitText = "&";
                if (query[0] == '$')
                {
                    query = query[1..];
                    splitText = "&$";
                }
                var key = string.Empty;
                foreach (var part in query.Split(splitText))
                {
                    int idx = part.IndexOf('=');
                    if (idx < 0)
                    {
                        queryOptions[key] += part;
                    }
                    else
                    {
                        key = part[..idx].ToLower();
                        queryOptions.Add(key, part[idx..]);
                    }
                }
                //ODataQueryParser parser = new(schema, q, queryOptions);
                //parser.
            }
            return q;
        }
        return null;
    }

    #endregion

    // -------------------------------------------------------------------------
    #region ** to text

    /// <summary>Gets SQL code for the query expression.</summary>
    /// <returns>The query SQL code.</returns>
    public virtual string ToSql()
    {
        return string.Empty;
    }

    /// <summary>Gets OData code for the query expression.</summary>
    /// <param name="url">The OData server URL.</param>
    /// <returns>The query OData code.</returns>
    public string ToOData(string url)
    {
        var sb = new StringBuilder();
        AppendSelect(sb);
        AppendFilter(sb);
        AppendExpand(sb);
        AppendOrderBy(sb);
        AppendTop(sb);
        AppendSkip(sb);
        if (sb.Length > 0) sb.Insert(0, '?');
        sb.Insert(0, url);
        return sb.ToString();
    }

    #endregion

    // -------------------------------------------------------------------------
    #region ** to text OData implementation

    void AppendSelect(StringBuilder sb)
    {
        if (Columns.Count == 0) return;
        if (sb.Length > 0) sb.Append('&');
        for (var i = 0; i < Columns.Count; i++)
        {
            if (i == 0)
                sb.Append("$select=");
            else
                sb.Append(',');
            sb.Append(Columns[i]);
        }
    }
    void AppendFilter(StringBuilder sb)
    {
        if (Criteria != null)
        {
            if (sb.Length > 0) sb.Append('&');
            AppendFilter(sb, Criteria);
        }
    }
    void AppendOrderBy(StringBuilder sb)
    {
        if (Orders.Count == 0) return;
        if (sb.Length > 0) sb.Append('&');
        for (var i = 0; i < Orders.Count; i++)
        {
            var order = Orders[i];
            if (i == 0)
                sb.Append("$orderby=");
            else
                sb.Append(',');
            sb.Append(order.ItemName);
            if (order.OrderType == XOrderType.Descending)
                sb.Append(' ').Append("desc");
        }
    }
    void AppendExpand(StringBuilder sb)
    {
        if (Links.Count == 0) return;
        if (sb.Length > 0) sb.Append('&');
        for (var i = 0; i < Links.Count; i++)
        {
            var link = Links[i];
            if (i == 0)
            {
                sb.Append("$expand=");
            }
            else
            {
                sb.Append(',');
            }
            if (EntityName != link.FromEntityName)
            {
                sb.Append(link.FromEntityName).Append('/');
            }
            sb.Append(link.ToEntityName).Append('.').Append(link.ToItemName);
        }
    }
    void AppendTop(StringBuilder sb)
    {
        var top = Top;
        if (top <= 0 && PageInfo != null)
        {
            top = PageInfo.Count;
        }
        if (top > 0)
        {
            if (sb.Length > 0) sb.Append('&');
            sb.Append("$top=").Append(top);
        }
    }
    void AppendSkip(StringBuilder sb)
    {
        var skip = Skip;
        if (skip <= 0 && PageInfo != null && PageInfo.PageNumber > 1)
        {
            skip = PageInfo.Count * (PageInfo.PageNumber - 1);
        }
        if (skip > 0)
        {
            if (sb.Length > 0) sb.Append('&');
            sb.Append("$skip=").Append(skip);
        }
    }

    void AppendFilter(StringBuilder sb, XFilter filter)
    {
        for (var i = 0; i < filter.Conditions.Count; i++)
        {
            if (i == 0)
                sb.Append("$filter=");
            else
                sb.Append(filter.FilterOperator == XLogicalOperator.And ? "and" : "or");
            AppendCondition(sb, filter.Conditions[i]);
        }
        for (var i = 0; i < filter.Filters.Count; i++)
        {
            sb.Append('(');
            AppendFilter(sb, filter.Filters[i]);
            sb.Append(')');
        }
    }
    void AppendCondition(StringBuilder sb, XCondition condition)
    {
        var field = condition.ItemName;
        var value = ToText(condition.Value);
        switch (condition.Operator)
        {
            case XConditionOperator.Equal:
                sb.Append(field).Append(' ').Append("eq").Append(' ').Append(value);
                break;
            case XConditionOperator.NotEqual:
                sb.Append(field).Append(' ').Append("ne").Append(' ').Append(value);
                break;
            case XConditionOperator.GreaterThan:
                sb.Append(field).Append(' ').Append("gt").Append(' ').Append(value);
                break;
            case XConditionOperator.GreaterEqual:
                sb.Append(field).Append(' ').Append("ge").Append(' ').Append(value);
                break;
            case XConditionOperator.LessThan:
                sb.Append(field).Append(' ').Append("lt").Append(' ').Append(value);
                break;
            case XConditionOperator.LessEqual:
                sb.Append(field).Append(' ').Append("le").Append(' ').Append(value);
                break;
            //case XConditionOperator.StartsWith:
            //    return 'startswith(' + field + ',' + value + ')';
            //case XConditionOperator.NotStartsWith:
            //    return 'not startswith(' + field + ',' + value + ')';
            //case XConditionOperator.EndsWith:
            //    return 'endswith(' + field + ',' + value + ')';
            //case XConditionOperator.NotEndsWith:
            //    return 'not endswith(' + field + ',' + value + ')';
            case XConditionOperator.Like:
                sb.Append("contains(").Append(field).Append(',').Append(value).Append(')');
                break;
            case XConditionOperator.NotLike:
                sb.Append("not contains(").Append(field).Append(',').Append(value).Append(')');
                break;
            case XConditionOperator.Null:
                sb.Append(field).Append(' ').Append("eq").Append(" null");
                break;
            case XConditionOperator.NotNull:
                sb.Append(field).Append(' ').Append("ne").Append(" null");
                break;
        }
    }

    #endregion
}

// -----------------------------------------------------------------------------
#region ** OData query parser

/// <summary>Parser for query.</summary>
internal class ODataQueryParser
{
    ISchema _schema;
    ITableSchema _tableSchema;
    readonly bool _useDollar = false;
    Dictionary<string, string> _options = new();

    public ODataQueryParser(ISchema schema, string table, string query)
    {
        var collection = schema.Tables as XCollection<ITableSchema, string>;
        _tableSchema = collection.GetBy(table.Trim(' ', '('));
        if (_tableSchema == null)
        {
            throw new XSchemaException($"Table {table} not found", table);
        }
        _schema = schema;
        query = query.Trim();
        if (!string.IsNullOrWhiteSpace(query))
        {
            var splitText = "&";
            if (query[0] == '$')
            {
                _useDollar = true;
                query = query[1..];
                splitText = "&$";
            }
            var key = string.Empty;
            foreach (var part in query.Split(splitText))
            {
                int idx = part.IndexOf('=');
                if (idx < 0)
                {
                    _options[key] += part;
                }
                else
                {
                    key = part[..idx].ToLower();
                    _options.Add(key, part[idx..]);
                }
            }
        }
    }

    /// <summary>
    /// ParseSelectAndExpand from an instantiated class
    /// </summary>
    /// <returns>A SelectExpandClause with the semantic representation of select and expand terms</returns>
    public string[] ParseSelect()
    {
        List<string> list = new();
        if (_options.TryGetValue("select", out string text))
        {
            // parse column names
            foreach (var part in text.Split(','))
            {
                // column name?
                var name = part.Trim();

                // compare with schema
                var collection = _tableSchema.Columns as XCollection<IColumnSchema, string>;
                var columnSchema = collection.GetBy(name);
                if (columnSchema == null)
                {
                    throw new XSchemaException($"Column {name} not found", name);
                }
                list.Add(name);
            }
        }
        return list.ToArray();
    }

    public IColumnSchema ParseExpand()
    {
        if (_options.TryGetValue("expand", out string text))
        {
            // parse column names
            foreach (var part in text.Split(','))
            {
                // table/column
                var ss = part.Trim().Split('/');
                var table = ss[0].Trim();
                if (string.IsNullOrWhiteSpace(table))
                {
                    throw new XSchemaException($"Incorrect expand", table);
                }
                var name = (ss.Length > 1) ? ss[1].Trim() : $"{table}Id";

                // test table
                var tables = _schema.Tables as XCollection<ITableSchema, string>;
                var tableSchema = tables.GetBy(table);
                if (tableSchema == null)
                {
                    throw new XSchemaException($"Table {table} not found", table);
                }

                // compare with schema
                var columns = _tableSchema.Columns as XCollection<IColumnSchema, string>;
                var columnSchema = columns.GetBy(name);
                if (columnSchema == null)
                {
                    throw new XSchemaException($"Column {name} not found", name);
                }
                if (table.Equals(columnSchema.ForeignTable))
                {
                    return columnSchema;
                }
            }
        }
        return null;
    }

    /// <summary>
    /// Parses an orderBy on the given full query.
    /// </summary>
    /// <returns>A <see cref="XOrderType"/> value.</returns>
    public bool ParseOrderBy(out IColumnSchema columnSchema, out XOrderType orderType)
    {
        columnSchema = null;
        orderType = (XOrderType)0xff;

        if (_options.TryGetValue("orderby", out string text))
        {
            // column
            orderType = (XOrderType)0xff;
            var ss = text.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (ss.Length > 1)
            {
                switch (ss[1].ToLower())
                {
                    case "asc":
                        orderType = XOrderType.Ascending;
                        break;
                    case "desc":
                        orderType = XOrderType.Descending;
                        break;
                }
            }

            // compare with schema
            var columns = _tableSchema.Columns as XCollection<IColumnSchema, string>;
            columnSchema = columns.GetBy(ss[0]);
            if (columnSchema == null || orderType == (XOrderType)0xff)
            {
                throw new XSchemaException($"Incorrect orderBy", text);
            }
            //return (columnSchema, orderType);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Parses a $top query option
    /// </summary>
    /// <returns>A value representing that top option, null if $top query does not exist.</returns>
    public bool ParseTop(out long value)
    {
        value = -1;
        if (!_options.TryGetValue("top", out string text))
        {
            return false;
        }
        if (!long.TryParse(text, out value))
        {
            throw new XSchemaException($"Incorrect top", text);
        }
        return true;
    }

    /// <summary>
    /// Parses a $skip query option.
    /// </summary>
    /// <param name="value">A skip value.</param>
    /// <returns>A value representing that skip option.</returns>
    /// <exception cref="XSchemaException">If incorrect skip.</exception>
    public bool ParseSkip(out long value)
    {
        value = -1;
        if (!_options.TryGetValue("skip", out string text))
        {
            return false;
        }
        if (!long.TryParse(text, out value))
        {
            throw new XSchemaException($"Incorrect skip", text);
        }
        return true;
    }

    ///// <summary>
    ///// Parses a $index query option
    ///// </summary>
    ///// <returns>A value representing that index option, null if $index query does not exist.</returns>
    //public long? ParseIndex()
    //{
    //    string indexQuery;
    //    return this.TryGetQueryOption(UriQueryConstants.IndexQueryOption, out indexQuery) ? ParseIndex(indexQuery) : null;
    //}

    ///// <summary>
    ///// Parses a $count query option
    ///// </summary>
    ///// <returns>A count representing that count option, null if $count query does not exist.</returns>
    //public bool? ParseCount()
    //{
    //    string countQuery;
    //    return this.TryGetQueryOption(UriQueryConstants.CountQueryOption, out countQuery) ? ParseCount(countQuery) : null;
    //}

    ///// <summary>
    ///// Parses the $search.
    ///// </summary>
    ///// <returns>SearchClause representing $search.</returns>
    //public SearchClause ParseSearch()
    //{
    //    if (this.searchClause != null)
    //    {
    //        return this.searchClause;
    //    }

    //    string searchQuery;
    //    if (!this.TryGetQueryOption(UriQueryConstants.SearchQueryOption, out searchQuery)
    //        || searchQuery == null)
    //    {
    //        return null;
    //    }

    //    this.searchClause = ParseSearchImplementation(searchQuery, this.Configuration);
    //    return searchClause;
    //}

    ///// <summary>
    ///// Parses a $skiptoken query option
    ///// </summary>
    ///// <returns>A value representing that skip token option, null if $skiptoken query does not exist.</returns>
    //public string ParseSkipToken()
    //{
    //    string skipTokenQuery;
    //    return this.TryGetQueryOption(UriQueryConstants.SkipTokenQueryOption, out skipTokenQuery) ? skipTokenQuery : null;
    //}
}

#endregion

// -----------------------------------------------------------------------------
#region ** option set and formatted value

/// <summary>
/// Represents a value for an attribute that has an option set.
/// </summary>
public partial class XOptionSetValue
{
    /// <summary>
    /// Initialization option set value.
    /// </summary>
    public XOptionSetValue()
    {
    }
    /// <summary>
    /// Initialization option set value.
    /// </summary>
    /// <param name="value">The current value.</param>
    public XOptionSetValue(int value)
    {
        Value = value;
    }

    /// <summary>
    /// Gets or sets the current value.
    /// </summary>
    public int Value { get; set; }

    /// <summary>
    /// Gets or sets the current value.
    /// </summary>
    public string Text { get; set; }

    /// <summary>
    /// Parse string value from ToString() method.
    /// </summary>
    /// <param name="text">The string value.</param>
    /// <returns>Create new <see cref="XOptionSetValue"/> object or <see cref="ArgumentOutOfRangeException"/>.</returns>
    public static XOptionSetValue Parse(string text)
    {
        if (!TryParse(text, out XOptionSetValue optionSet))
        {
            throw new ArgumentOutOfRangeException("text");
        }
        return optionSet;
    }
    /// <summary>
    /// Try parse string value from ToString() method.
    /// </summary>
    /// <param name="text">The string value.</param>
    /// <param name="optionSet">The new <see cref="XOptionSetValue"/> object>, otherwise <b>null</b>.</param>
    /// <returns>Success flag.</returns>
    public static bool TryParse(string text, out XOptionSetValue optionSet)
    {
        // sanity
        optionSet = null;
        if (string.IsNullOrEmpty(text)) return false;

        // sanity first
        if (text[0] != '#') return false;

        // search
        var s = text.ToUpper();
        int setIdx = s.IndexOf("#SET=");

        // sanity
        if (setIdx != 0) return false;
        int txtIdx = s.IndexOf("/#TXT=");
        if (txtIdx < 0) txtIdx = text.Length;

        // parse
        if (int.TryParse(text.AsSpan(5, txtIdx - 5), out int value))
        {
            optionSet = new XOptionSetValue(value);
            if (txtIdx > 0 && txtIdx < text.Length)
            {
                optionSet.Text = text[(txtIdx + 6)..].Trim();
            }
        }

        // done
        return optionSet != null;
    }

    /// <summary>
    /// Serves as a hash function for a particular type.
    /// </summary>
    /// <returns>A hash code for the current object.</returns>
    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

    /// <summary>
    /// Determines whether the specified <see cref="object"/> is equal to the current <see cref="object"/>.
    /// </summary>
    /// <param name="obj">The object to compare with the current object.</param>
    /// <returns><b>true</b> if the specified <see cref="object"/> is equal to the current <see cref="object"/>, otherwise, <b>false</b>.</returns>
    public override bool Equals(object obj)
    {
        if (obj != null && obj is XOptionSetValue)
        {
            var other = (XOptionSetValue)obj;
            return Value == other.Value;
        }
        return base.Equals(obj);
    }

    /// <summary>
    /// Returns a string that represents the current object.
    /// </summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
    {
        if (string.IsNullOrEmpty(Text))
            return string.Format(@"#SET={0}", Value);
        return string.Format(@"#SET={0}/#TXT={1}", Value, Text);
    }
}

/// <summary>
/// Represents a formatted value.
/// </summary>
public partial class XFormattedValue
{
    /// <summary>
    /// Initialization formatted value.
    /// </summary>
    /// <param name="format">The .NET format string.</param>
    /// <param name="value">The current value.</param>
    public XFormattedValue(string format, object value)
    {
        Format = format;
        Value = value;
    }

    /// <summary>
    /// Gets or sets the .NET format string for value.
    /// </summary>
    public string Format { get; set; }
    /// <summary>
    /// Gets or sets the current value.
    /// </summary>
    public object Value { get; set; }

    /// <summary>
    /// Serves as a hash function for a particular type.
    /// </summary>
    /// <returns>A hash code for the current object.</returns>
    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
    /// <summary>
    /// Determines whether the specified <see cref="object"/> is equal to the current <see cref="object"/>.
    /// </summary>
    /// <param name="obj">The object to compare with the current object.</param>
    /// <returns><b>true</b> if the specified <see cref="object"/> is equal to the current <see cref="object"/>, otherwise, <b>false</b>.</returns>
    public override bool Equals(object obj)
    {
        if (obj != null && obj is XFormattedValue)
        {
            var other = (XFormattedValue)obj;
            return Format == other.Format && Value == other.Value;
        }
        return base.Equals(obj);
    }

    /// <summary>
    /// Returns a string that represents the current object.
    /// </summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
    {
        if (Value == null)
        {
            return string.Empty;
        }
        var text = Value.ToString();
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(Format))
        {
            return text;
        }
        return string.Format(Format, text);
    }
}

#endregion

// -----------------------------------------------------------------------------
#region ** collection extensions

public static class CollectionExtensions
{
    public static TValue GetItem<TKey, TValue>(this IList<KeyValuePair<TKey, TValue>> collection, TKey key)
        where TKey : class
    {
        TValue value;
        if (TryGetValue(collection, key, out value))
        {
            return value;
        }
        return default(TValue);
    }

    public static void SetItem<TKey, TValue>(this IList<KeyValuePair<TKey, TValue>> collection, TKey key,
                                             TValue value) where TKey : class
    {
        int index;
        if (TryGetIndex(collection, key, out index))
        {
            collection.RemoveAt(index);
        }

        // if the value is an array, it needs to be converted into a List. This is due to how Silverlight serializes
        // Arrays and IList<T> objects (they are both serialized with the same namespace). Any collection objects will
        // already add the KnownType for IList<T>, which means that any parameters that are arrays cannot be added
        // as a KnownType (or it will throw an exception).
        var array = value as Array;
        if (null != array)
        {
            Type listType =
                typeof(List<>).GetGenericTypeDefinition().MakeGenericType(array.GetType().GetElementType());
            object list = Activator.CreateInstance(listType, array);
            try
            {
                value = (TValue)list;
            }
            catch (InvalidCastException)
            {
                //Don't do the conversion because the types are not compatible
            }
        }
#if SILVERLIGHT
        collection.Add(new KeyValuePair<TKey, TValue> { Key = key, Value = value });
#else
        collection.Add(new KeyValuePair<TKey, TValue>(key, value));
#endif
    }

    public static bool ContainsKey<TKey, TValue>(this IList<KeyValuePair<TKey, TValue>> collection, TKey key)
        where TKey : class
    {
        int index;
        return TryGetIndex(collection, key, out index);
    }

    public static bool TryGetValue<TKey, TValue>(this IList<KeyValuePair<TKey, TValue>> collection, TKey key,
                                                 out TValue value) where TKey : class
    {
        int index;
        if (TryGetIndex(collection, key, out index))
        {
            value = collection[index].Value;
            return true;
        }

        value = default(TValue);
        return false;
    }

    private static bool TryGetIndex<TKey, TValue>(IList<KeyValuePair<TKey, TValue>> collection, TKey key,
                                                  out int index) where TKey : class
    {
        if (collection == null || key == null)
        {
            index = -1;
            return false;
        }

        index = -1;
        for (int i = 0; i < collection.Count; i++)
        {
            if (key.Equals(collection[i].Key))
            {
                index = i;
                return true;
            }
        }

        return false;
    }
    public static void Add<TKey, TValue>(this IList<KeyValuePair<TKey, TValue>> collection, TKey key,
                                         TValue value) where TKey : class
    {
#if SILVERLIGHT
        collection.Add(new KeyValuePair<TKey, TValue> { Key = key, Value = value });
#else
        collection.Add(new KeyValuePair<TKey, TValue>(key, value));
#endif
    }

}

#endregion
