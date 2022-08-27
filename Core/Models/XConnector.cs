using System;
using System.Data;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Diagnostics;
using System.Threading;

namespace Uniya.Core;

/// <summary>
/// Base connector support.
/// </summary>
public abstract class XConnector : IConnetor
{
    // -------------------------------------------------------------------------------
    #region ** connector

    /// <summary>Gets or sets connection object.</summary>
    public IConnection Connection
    {
        get; set;
    }

    #endregion

    // -------------------------------------------------------------------------------
    #region ** SQL script model

    ///// <summary>Gets script code of some model.</summary>
    ///// <param name="types">The types of the model.</param>
    ///// <returns>The SQL initialization script.</returns>

    /// <summary>
    /// Gets schema of some timed model in the assembly.
    /// </summary>
    /// <param name="assembly">The assemble with timed model, by default this.</param>
    /// <returns>The schema of assembly model.</returns>
    public static ISchema GetSchema(Assembly assembly = null)
    {
        // initialization
        var schema = new XSchema();
        if (assembly == null)
        {
            assembly = Assembly.GetExecutingAssembly();
        }

        // do stuff columns
        var tables = new Dictionary<Type, Dictionary<string, IColumnSchema>>();
        var indexes = new Dictionary<Type, Dictionary<string, List<IColumnSchema>>>();
                  
        // all timed classes
        foreach (Type type in assembly.GetTypes().Where(type => type.GetInterfaces().Contains(typeof(IDB))))
        {
            // ignore not interfaces
            if (!type.IsInterface) continue;

            // ignore common interfaces
            if (type.Name.EndsWith("DB")) continue;

            // do stuff columns
            var tableColumns = new Dictionary<string, IColumnSchema>();
            tables.Add(type, tableColumns);
            var indexColumns = new Dictionary<string, List<IColumnSchema>>();
            indexes.Add(type, indexColumns);

            // columns
            //List<XColumn> columns = new List<XColumn>();
            //var members = type.GetMembers(BindingFlags.Public | BindingFlags.Instance);
            var members = type.GetPublicProperties();
            foreach (MemberInfo mi in members)
            {
                //int order = -1;
                var pi = mi as PropertyInfo;
                if (pi != null)
                {
                    // ignore not browser columns
                    var browsable = (BrowsableAttribute)Attribute.GetCustomAttribute(mi, typeof(BrowsableAttribute));
                    if (browsable != null && !browsable.Browsable) continue;

                    // create column schema
                    var column = new XColumnSchema() { ColumnName = mi.Name };
                    column.DataType = XSchema.GetDataType(pi.PropertyType);

                    // get display name of the column
                    var dna = (DisplayNameAttribute)Attribute.GetCustomAttribute(mi, typeof(DisplayNameAttribute));
                    if (dna != null)
                    {
                        column.Title = dna.DisplayName;
                    }
                    else
                    {
                        var dia = (DisplayAttribute)Attribute.GetCustomAttribute(mi, typeof(DisplayAttribute));
                        if (dia != null)
                        {
                            if (string.IsNullOrWhiteSpace(dia.ShortName))
                            {
                                column.Title = dia.Name;
                            }
                            else
                            {
                                column.Title = dia.ShortName;
                                column.Description = dia.Name;
                            }
                            if (string.IsNullOrWhiteSpace(dia.Description))
                            {
                                column.Description = dia.Description;
                            }
                            if (string.IsNullOrWhiteSpace(dia.Description))
                            {
                                column.Description = dia.Prompt;
                            }
                            column.Order = dia.Order;
                        }
                    }

                    // get description (help) of the column
                    var da = (DescriptionAttribute)Attribute.GetCustomAttribute(mi, typeof(DescriptionAttribute));
                    if (da != null) column.Description = da.Description;

                    // get read only or no for the column
                    var roa = (ReadOnlyAttribute)Attribute.GetCustomAttribute(mi, typeof(ReadOnlyAttribute));
                    if (roa != null) column.Requirement |= XRequirementOptions.ReadOnly;

                    // get key for the column
                    var ka = (KeyAttribute)Attribute.GetCustomAttribute(mi, typeof(KeyAttribute));
                    if (ka != null) column.Requirement |= XRequirementOptions.PrimaryKey;

                    // get regular expression for the column
                    var rea = (RegularExpressionAttribute)Attribute.GetCustomAttribute(mi, typeof(RegularExpressionAttribute));
                    if (rea != null) column.Pattern = rea.Pattern;
                    var pha = (PhoneAttribute)Attribute.GetCustomAttribute(mi, typeof(PhoneAttribute));
                    if (pha != null && string.IsNullOrWhiteSpace(column.Pattern))
                    {
                        // phone
                        column.Pattern = @"^((8|\+7)[\- ]?)?(\(?\d{3}\)?[\- ]?)?[\d\- ]{7,10}$";
                    }
                    var ema = (EmailAddressAttribute)Attribute.GetCustomAttribute(mi, typeof(EmailAddressAttribute));
                    if (ema != null && string.IsNullOrWhiteSpace(column.Pattern))
                    {
                        // email
                        column.Pattern = @"^([a-z0-9_\.-]+)@([a-z0-9_\.-]+)\.([a-z\.]{2,6})$";
                    }
                    var ссla = (CreditCardAttribute)Attribute.GetCustomAttribute(mi, typeof(CreditCardAttribute));
                    if (ссla != null && string.IsNullOrWhiteSpace(column.Pattern))
                    {
                        // credit card
                        column.Pattern = @"^(?:4[0-9]{12}(?:[0-9]{3})?|5[1-5][0-9]{14}|6(?:011|5[0-9][0-9])[0-9]{12}|3[47][0-9]{13}|3(?:0[0-5]|[68][0-9])[0-9]{11}|(?:2131|1800|35\d{3})\d{11})$";
                    }
                    var urla = (UrlAttribute)Attribute.GetCustomAttribute(mi, typeof(UrlAttribute));
                    if (urla != null && string.IsNullOrWhiteSpace(column.Pattern))
                    {
                        // URL
                        column.Pattern = @"^(https?:\/\/)?([\da-z\.-]+)\.([a-z\.]{2,6})([\/\w \.-]*)*\/?$";
                    }
                    //var ra = (RangeAttribute)Attribute.GetCustomAttribute(mi, typeof(RangeAttribute));
                    //if (ra != null && string.IsNullOrWhiteSpace(column.Pattern))
                    //{
                    //    // value range
                    //}

                    // get data type for the column
                    var dta = (DataTypeAttribute)Attribute.GetCustomAttribute(mi, typeof(DataTypeAttribute));
                    if (dta != null && string.IsNullOrWhiteSpace(column.Pattern))
                    {
                        switch (dta.DataType)
                        {
                            case DataType.CreditCard:
                                column.Pattern = @"^(?:4[0-9]{12}(?:[0-9]{3})?|5[1-5][0-9]{14}|6(?:011|5[0-9][0-9])[0-9]{12}|3[47][0-9]{13}|3(?:0[0-5]|[68][0-9])[0-9]{11}|(?:2131|1800|35\d{3})\d{11})$";
                                break;
                            case DataType.Currency:
                                // TODO:
                                break;
                            case DataType.Custom:
                                break;
                            case DataType.Date:
                                // TODO:
                                break;
                            case DataType.DateTime:
                                // TODO:
                                break;
                            case DataType.Duration:
                                break;
                            case DataType.EmailAddress:
                                column.Pattern = @"^([a-z0-9_\.-]+)@([a-z0-9_\.-]+)\.([a-z\.]{2,6})$";
                                break;
                            case DataType.Html:
                                break;
                            case DataType.ImageUrl:
                                break;
                            case DataType.MultilineText:
                                break;
                            case DataType.Password:
                                column.Pattern = @"(/^(?=.*\d)(?=.*[a-z])(?=.*[A-Z])[0-9a-zA-Z]{8,}$/)";
                                break;
                            case DataType.PhoneNumber:
                                column.Pattern = @"^((8|\+7)[\- ]?)?(\(?\d{3}\)?[\- ]?)?[\d\- ]{7,10}$";
                                break;
                            case DataType.PostalCode:
                                column.Pattern = @"^\d{5}(?:[-\s]\d{4})?$";
                                break;
                            case DataType.Text:
                                break;
                            case DataType.Time:
                                // TODO:
                                break;
                            case DataType.Upload:
                                break;
                            case DataType.Url:
                                column.Pattern = @"^(https?:\/\/)?([\da-z\.-]+)\.([a-z\.]{2,6})([\/\w \.-]*)*\/?$";
                                break;
                        }
                        if (dta.RequiresValidationContext)
                        {
                            column.Requirement |= XRequirementOptions.Required;
                        }
                    }

                    // get minimal and maximal length for the column
                    int min = int.MaxValue;
                    int max = int.MinValue;
                    var mila = (MinLengthAttribute)Attribute.GetCustomAttribute(mi, typeof(MinLengthAttribute));
                    if (mila != null) min = mila.Length;
                    var mala = (MaxLengthAttribute)Attribute.GetCustomAttribute(mi, typeof(MaxLengthAttribute));
                    if (mala != null) max = mala.Length;
                    var sla = (StringLengthAttribute)Attribute.GetCustomAttribute(mi, typeof(StringLengthAttribute));
                    if (sla != null)
                    {
                        min = Math.Max(min, sla.MinimumLength);
                        max = Math.Min(max, sla.MaximumLength);
                    }
                    if (min != int.MaxValue)
                    {
                        column.Length = Math.Max(column.Length, min);
                        if (string.IsNullOrWhiteSpace(column.Pattern))
                        {
                            if (max != int.MinValue)
                                column.Pattern = $"/{{{min},{max}}}/g";
                            else
                                column.Pattern = $"/{{{min},}}/g";
                        }
                    }
                    else if (max != int.MinValue)
                    {
                        column.Pattern = $"/{{,{max}}}/g";
                    }

                    // get maximal length for the column
                    if (mala != null && string.IsNullOrWhiteSpace(column.Pattern))
                    {
                        if (string.IsNullOrWhiteSpace(column.Pattern))
                            column.Pattern = $"/{{,{mala.Length}}}/g";
                    }

                    //var ra = (Att)Attribute.GetCustomAttribute(mi, typeof(RequiredAttribute));

                    // get required for the column
                    var ra = (RequiredAttribute)Attribute.GetCustomAttribute(mi, typeof(RequiredAttribute));
                    if (ra != null)
                    {
                        column.Requirement |= XRequirementOptions.Required;
                        if (!ra.AllowEmptyStrings)
                            column.Requirement |= XRequirementOptions.NotNull;
                    }

                    // get unique and required for the column
                    var ua = (UniqueAttribute)Attribute.GetCustomAttribute(mi, typeof(UniqueAttribute));
                    if (ua != null)
                    {
                        var group = ua.Group;
                        column.Requirement |= XRequirementOptions.Required;
                        if (string.IsNullOrEmpty(group))
                            column.Requirement |= XRequirementOptions.UniqueKey;
                        if (!indexColumns.ContainsKey(group))
                            indexColumns.Add(group, new List<IColumnSchema>());
                        indexColumns[group].Add(column);
                    }

                    // foreign table
                    var fka = (ForeignKeyAttribute)Attribute.GetCustomAttribute(mi, typeof(ForeignKeyAttribute));
                    if (fka != null)
                    {
                        column.ForeignTable = fka.Name;
                    }

                    // get enumeration data type for the column
                    var edta = (EnumDataTypeAttribute)Attribute.GetCustomAttribute(mi, typeof(EnumDataTypeAttribute));
                    if (edta != null)
                    {
                        var enumType = edta.EnumType;
                        foreach (var item in Enum.GetValues(enumType))
                        {
                            var name = item.ToString();
                            var option = new XOptionSchema()
                            {
                                Value = Convert.ToInt32(item)
                            };
                            var eda = enumType.GetMember(name).First().GetCustomAttribute<DisplayAttribute>();
                            if (eda != null)
                            {
                                option.Description = eda.Description;
                                name = eda.Name;
                            }
                            var edna = enumType.GetMember(name).First().GetCustomAttribute<DisplayNameAttribute>();
                            if (edna != null) name = edna.DisplayName;
                            option.Title = name;
                            column.OptionSet.Add(option);
                        }
                    }

                    // add column
                    tableColumns.Add(column.ColumnName, column);
                }
            }
        }
        foreach (var type in tables.Keys)
        {
            // sanity table name
            var tableName = type.Name;
            if (tableName.StartsWith("I"))
            {
                // object name without prefix for interface
                tableName = tableName.Substring(1);
            }

            // initialization
            var columns = tables[type];
            var indexColumns = indexes[type];
            var table = new XTableSchema() { TableName = tableName };

            // primary key
            var primaryKey = new XColumnSchema()
            {
                ColumnName = "Id",
                Title = "Identifier",
                DataType = XDataType.Int64,
                Requirement = XRequirementOptions.PrimaryKey,
            };
            table.Columns.Add(primaryKey);
            table.PrimaryKey = primaryKey.ColumnName;

            // add table with columns
            foreach (var key in columns.Keys)
            {
                table.Columns.Add(columns[key]);
            }

            // indexes
            if (indexColumns.Count > 0)
            {
                foreach (var key in indexColumns.Keys)
                {
                    var index = new XIndexSchema();
                    index.TableName = tableName;
                    var indexName = key;
                    index.SchemaName = schema.SchemaName;
                    foreach (var column in indexColumns[key])
                    {
                        if (string.IsNullOrEmpty(key))
                            indexName += column.ColumnName;
                        index.Columns.Add(column);
                    }
                    index.IndexName = indexName;
                    table.Indexes.Add(index);
                }
            }

            // add to schema
            schema.Tables.Add(table);
        }

        // done
        return schema;
    }

    #endregion
}

/// <summary>
/// SQL common connector support.
/// </summary>
public abstract class SqlConnector : XConnector
{
    // ------------------------------------------------------------------------------------
    #region ** CRUD model

    /// <summary>Gets or sets weather parameter with name or no.</summary>
    protected bool ParameterWithName
    {
        get; set;
    }
    /// <summary>Gets or sets parameter char.</summary>
    protected char ParameterChar
    {
        get; set;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="schema"></param>
    /// <returns></returns>
    protected string GetInsert(ITableSchema schema)
    {
        var sb = new StringBuilder($"INSERT INTO {schema.TableName} (");
        var values = new StringBuilder();
        foreach (var column in schema.Columns)
        {
            if (values.Length > 0)
            {
                values.Append(',');
                sb.Append(',');
            }
            values.Append(ParameterChar);
            if (ParameterWithName)
            {
                values.Append(column.ColumnName);
            }
            sb.Append(column.ColumnName);
        }
        sb.Append(')').Append(" VALUES (").Append(values).Append(')');
        return sb.ToString();
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="cmd"></param>
    /// <param name="entity"></param>
    /// <returns></returns>
    protected bool CmdInsert(IDbCommand cmd, XEntity entity)
    {
        // sanity
        if (entity.State != XEntityState.Created || entity.Items.Count == 0) return false;

        // parameters
        cmd.Parameters.Clear();
        var sb = new StringBuilder($"INSERT INTO {entity.EntityName} (");
        var values = new StringBuilder();
        foreach (var item in entity.Items)
        {
            if (values.Length > 0)
            {
                values.Append(',');
                sb.Append(',');
            }
            var parameterName = new string(ParameterChar, 1);
            var parameter = cmd.CreateParameter();
            if (ParameterWithName)
            {
                parameterName += item.Key;
                parameter.ParameterName = parameterName;
            }
            values.Append(parameterName);
            parameter.Value = item.Value;
            cmd.Parameters.Add(parameter);
            sb.Append(item.Key);
        }
        sb.Append(')').Append(" VALUES (").Append(values).Append(')');
        cmd.CommandText = sb.ToString();

        // done
        return true;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="cmd"></param>
    /// <param name="entity"></param>
    /// <returns></returns>
    protected bool CmdUpdate(IDbCommand cmd, XEntity entity)
    {
        // sanity
        if (entity.State != XEntityState.Modified || entity.OldItems.Count == 0) return false;

        // parameters
        cmd.Parameters.Clear();
        var sb = new StringBuilder();

        // changed items
        foreach (var item in entity.OldItems)
        {
            // sanity
            var value = entity[item.Key];
            Debug.WriteLineIf(!XSchema.IsEqualTypes(value, item.Value), $"Type mismatch for {entity.EntityName}:{item.Key}");

            // next parameter
            if (sb.Length > 0) sb.Append(',');

            // add parameter
            var parameterName = new string(ParameterChar, 1);
            var parameter = cmd.CreateParameter();
            if (ParameterWithName)
            {
                parameterName += item.Key;
                parameter.ParameterName = parameterName;
            }
            parameter.Value = value;
            cmd.Parameters.Add(parameter);
            sb.Append(item.Key).Append('=').Append(parameterName);
        }

        // key
        var primaryKey = new string(ParameterChar, 1);
        var keyParameter = cmd.CreateParameter();
        if (ParameterWithName)
        {
            primaryKey += entity.PrimaryKey;
            keyParameter.ParameterName = primaryKey;
        }
        keyParameter.Value = entity.EntityId;
        cmd.Parameters.Add(keyParameter);

        // update
        sb.Insert(0, $"UPDATE {entity.EntityName} SET ");
        sb.Append(" WHERE ").Append(entity.PrimaryKey).Append('=').Append(primaryKey);
        cmd.CommandText = sb.ToString();

        // done
        return true;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="entityName"></param>
    /// <param name="primaryKey"></param>
    /// <returns></returns>
    protected string GetDelete(string entityName, string primaryKey)
    {
        var sb = new StringBuilder("DELETE ");
        sb.Append(entityName).Append(" WHERE ").Append(primaryKey).Append('=').Append(ParameterChar);
        if (ParameterWithName)
        {
            sb.Append(primaryKey);
        }
        return sb.ToString();
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="schema"></param>
    /// <returns></returns>
    protected string GetDelete(ITableSchema schema)
    {
        return GetDelete(schema.TableName, schema.PrimaryKey);
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="cmd"></param>
    /// <param name="entity"></param>
    /// <returns></returns>
    protected bool CmdDelete(IDbCommand cmd, XEntity entity)
    {
        // sanity
        if (entity.State == XEntityState.Created || entity.EntityId.Length == 0) return false;

        // parameters
        cmd.Parameters.Clear();
        cmd.CommandText = GetDelete(entity.EntityName, entity.PrimaryKey);
        cmd.Parameters.Add(entity[entity.PrimaryKey]);

        // done
        return true;
    }

    #endregion

    // ------------------------------------------------------------------------------------
    #region ** read entities

    /// <summary>Read entity by table schema.</summary>
    /// <param name="schema"></param>
    /// <param name="reader"></param>
    /// <param name="testing"></param>
    /// <returns></returns>
    protected XEntity ReadEntity(ITableSchema schema, IDataReader reader, bool testing = true)
    {
        // initialization
        var entity = new XEntity(schema);
        var columns = new Dictionary<string, XDataType>();

        // schema columns
        if (testing)
        {
            for (int i = 0; i < schema.Columns.Count; i++)
            {
                var columnName = schema.Columns[i].ColumnName;
                if (columns.ContainsKey(columnName))
                {
                    throw new XSchemaException($"Double columns with name: {columnName}", columnName);
                }
                columns.Add(columnName, schema.Columns[i].DataType);
            }
        }

        // read and test
        for (int i = 0; i < reader.FieldCount; i++)
        {
            var itemName = reader.GetName(i);
            if (testing && !columns.ContainsKey(itemName))
            {
                throw new XSchemaException($"Column {itemName} not found", itemName);
            }
            var value = reader.GetValue(i);
            if (testing && value != null && XSchema.GetDataType(value.GetType()) != columns[itemName])
            {
                throw new XSchemaException($"Mismatch data type for column {itemName}", itemName, columns[itemName]);
            }
            entity.Items.Add(itemName, value);
        }

        // done
        return entity;
    }
    /// <summary>Read entity by name.</summary>
    /// <param name="entityName"></param>
    /// <param name="reader"></param>
    /// <returns></returns>
    protected XEntity ReadEntity(string entityName, IDataReader reader)
    {
        var entity = new XEntity(entityName);
        for (int i = 0; i < reader.FieldCount; i++)
        {
            entity.Items.Add(reader.GetName(i), reader.GetValue(i));
        }
        return entity;
    }

    #endregion

    // ------------------------------------------------------------------------------------
    #region ** static object model

    /// <summary>
    /// Gets common output folder.
    /// </summary>
    public static string OutputFolder
    {
        get { return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location); }
    }
    /// <summary>
    /// Gets database output folder.
    /// </summary>
    public static string DataFolder
    {
        get { return Path.Combine(OutputFolder, "Data"); }
    }
    /// <summary>
    /// Gets scripts output folder with TypeScript or JavaScript code.
    /// </summary>
    public static string ScriptsFolder
    {
        get { return Path.Combine(OutputFolder, "Scripts"); }
    }
    #endregion
}

/// <summary>
/// Used on an EntityFramework Entity class to mark a property to be used as a Unique Key
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
public class UniqueAttribute : ValidationAttribute
{
    // ------------------------------------------------------------------------------------
    #region ** object model

    /// <summary>Marker attribute for unique value.</summary>
    /// <param name="group">Optional, group name of multiple properties that combined.</param>
    public UniqueAttribute(string group = null)
    {
        Group = group ?? string.Empty;
    }

    /// <summary>Gets group name of multiple properties that combined.</summary>
    public string Group { get; private set; }

    #endregion
}
