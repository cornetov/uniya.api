using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Data;
using System.Globalization;
using System.Reflection;
using System.Diagnostics;

using Microsoft.Data.SqlClient;

using Uniya.Core;

namespace Uniya.Connectors.MsSql;

/// <summary>MS SQL connector.</summary>
public class MsSqlConnector : SqlConnector, ITransactedData
{
    // -------------------------------------------------------------------------------
    #region ** fields & contractor

    private string _connectionString;
    private ISchema _schema;

    /// <summary>
    /// Connect to SQLite server.
    /// </summary>
    /// <param name="connection"></param>
    public MsSqlConnector(IConnection connection)
    {
        this.Connection = connection;
    }
    /// <summary>
    /// Connect to MS SQL server.
    /// </summary>
    /// <param name="connectionString"></param>
    public MsSqlConnector(string connectionString)
    {
        _connectionString = connectionString;
    }

    #endregion

    // -------------------------------------------------------------------------------
    #region ** select

    /// <summary>
    /// Read one entity using identifier with all columns.
    /// </summary>
    /// <param name="entityName">The entity name.</param>
    /// <param name="pairs">The pair of column name and value.</param>
    /// <returns>The entity collection.</returns>
    public async Task<XEntityCollection> Read(string entityName, params KeyValuePair<string, object>[] pairs)
    {
        // empty initialization
        var collection = new XEntityCollection();

        // read selected
        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            var cmd = connection.CreateCommand();
            cmd.CommandText = $"SELECT * FROM [dbo].[{entityName}]";
            var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                collection.Add(ReadEntity(entityName, reader));
            }
        }

        // done
        return collection;
    }

    /// <summary>
    /// Select data using query object.
    /// </summary>
    /// <param name="query">The query object.</param>
    /// <returns>The entity collection.</returns>
    public async Task<XEntityCollection> Select(XQuery query)
    {
        // empty initialization
        var collection = new XEntityCollection();

        // read selected
        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            var cmd = connection.CreateCommand();
            cmd.CommandText = query.ToSql();
            var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                collection.Add(Read(query.EntityName, reader));
            }
        }

        // done
        return collection;
    }

    XEntity Read(string entityName, IDataReader reader)
    {
        var entity = new XEntity(entityName);
        for (int i = 0; i < reader.FieldCount; i++)
        {
            entity.Items.Add(reader.GetName(i), reader.GetValue(i));
        }
        return entity;
    }

    #endregion

    //-----------------------------------------------------------------------------
    #region ** create

    /// <summary>
    /// Create entity object in database.
    /// </summary>
    /// <param name="entities">The collection of entity.</param>
    /// <returns>Without information.</returns>
    public async Task Create(params XEntity[] entities)
    {
        // sanity
        if (entities.Length == 0) return;

        // inserts
        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            var cmd = connection.CreateCommand();
            var entityName = string.Empty;

            // TODO: INSERT BULK

            foreach (var entity in entities)
            {
                // sanity
                if (entity.State != XEntityState.Created) continue;

                // create insert command and add parameters
                if (CmdInsert(cmd, entity))
                {
                    // execute
                    var id = await cmd.ExecuteScalarAsync();

                    // last settings
                    entity.EntityId = id.ToString();
                    entity.Actualization();
                }
            }
        }
    }

    #endregion

    // -------------------------------------------------------------------------------
    #region ** update

    /// <summary>
    /// Update entity object in database.
    /// </summary>
    /// <param name="entities">The collection of entity.</param>
    /// <returns>Without information.</returns>
    public async Task Update(params XEntity[] entities)
    {
        // sanity
        if (entities.Length == 0) return;

        // updates
        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            var cmd = connection.CreateCommand();
            var entityName = string.Empty;

            // TODO: UPDATE BULK

            foreach (var entity in entities)
            {
                // sanity
                if (entity.State != XEntityState.Created) continue;

                // create update command and add parameters
                if (CmdUpdate(cmd, entity))
                {
                    // execute
                    await cmd.ExecuteScalarAsync();

                    // last settings
                    entity.Actualization();
                }
            }
        }
    }

    #endregion

    // -------------------------------------------------------------------------------
    #region ** delete

    /// <summary>
    /// Delete entity object in database.
    /// </summary>
    /// <param name="entities">The collection of entity.</param>
    /// <returns>Without information.</returns>
    public async Task Delete(params XEntity[] entities)
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            var cmd = connection.CreateCommand();
            var entityName = string.Empty;
            foreach (var entity in entities)
            {
                if (CmdDelete(cmd, entity))
                    await cmd.ExecuteNonQueryAsync();
            }
        }
    }

    /// <summary>
    /// Delete entity object in database.
    /// </summary>
    /// <param name="entityName">The entity name.</param>
    /// <param name="key">The primary key name.</param>
    /// <param name="ids">The collection of identifiers.</param>
    /// <returns>Without information.</returns>
    public async Task Delete(string entityName, string key, params object[] ids)
    {
        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            var cmd = connection.CreateCommand();
            cmd.CommandText = GetDelete(entityName, key);
            foreach (var id in ids)
            {
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@ID", id);
                await cmd.ExecuteNonQueryAsync();
            }
        }
    }

    #endregion

    //-----------------------------------------------------------------------------
    #region ** transaction

    /// <summary>
    /// Apply transaction in database.
    /// </summary>
    /// <param name="iset">The entity set for transaction.</param>
    /// <returns>Without information.</returns>
    public async Task Transaction(IEntitySet iset)
    {
        using (var connection = new SqlConnection(ConnectionString))
        {
            await connection.OpenAsync();
            using (var transaction = connection.BeginTransaction())
            {
                var cmd = connection.CreateCommand();

                // create
                foreach (var entity in iset.Creating)
                {
                    if (CmdInsert(cmd, entity))
                    {
                        // execute
                        var id = await cmd.ExecuteScalarAsync();

                        // last settings
                        if (id != null)
                            entity.EntityId = id.ToString();
                        entity.Actualization();
                    }
                }

                // update
                foreach (var entity in iset.Updating)
                {
                    if (CmdUpdate(cmd, entity))
                    {
                        // execute
                        await cmd.ExecuteScalarAsync();

                        // last settings
                        entity.Actualization();
                    }
                }


                // delete
                foreach (var entity in iset.Deleting)
                {
                    if (CmdDelete(cmd, entity))
                        await cmd.ExecuteNonQueryAsync();
                }

                // commit changes
                transaction.Commit();
            }
        }
    }

    #endregion

    // -------------------------------------------------------------------------------
    #region ** schema

    /// <summary>
    /// Gets schema of the data.
    /// </summary>
    /// <param name="tableNames">The list of used table's names.</param>
    /// <returns>The schema of the data.</returns>
    public async Task<ISchema> GetSchema(params string[] tableNames)
    {
        return await ReadSchema(tableNames);
    }
    /// <summary>
    /// Sets table schema of the database.
    /// </summary>
    /// <param name="tableSchema">The new or changed schema of the table.</param>
    /// <returns>The changed schema of the data.</returns>
    public async Task<ISchema> SetSchema(ITableSchema tableSchema)
    {
        return await ReadSchema(tableSchema.TableName);
    }

    #endregion

    // -------------------------------------------------------------------------------
    #region ** individual object's model

    internal string ConnectionString
    {
        get { return this.Connection != null ? this.Connection.ComplexCode : _connectionString; }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="tableNames"></param>
    /// <returns></returns>
    public async Task<ISchema> ReadSchema(params string[] tableNames)
    {
        // required?
        if (_schema == null || DateTime.Now.Subtract(_schema.CreatedTime) > TimeSpan.FromMinutes(5))
        {
            // create
            _schema = new XSchema();

            // table's cache
            var tables = new Dictionary<string, int>();

            // build SQL query
            var sql = BuildSql(tableNames);

            // read selected
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var cmd = connection.CreateCommand();
                cmd.CommandText = sql;
                var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var column = new XColumnSchema();
                    var requirement = XRequirementOptions.None;

                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        var name = reader.GetName(i);
                        var value = reader.GetValue(i);
                        switch (name)
                        {
                            case "IsPrimaryKey":
                                if (Convert.ToBoolean(value))
                                    requirement |= XRequirementOptions.PrimaryKey;
                                break;
                            case "Nullable":
                                if (!Convert.ToBoolean(value))
                                    requirement |= XRequirementOptions.NotNull;
                                break;
                            case "DataType":
                                column.DataType = XEntity.GetDataType(value.ToString());
                                break;
                            default:
                                XProxy.SetValue(column, name, value);
                                break;
                        }
                    }
                    column.Requirement = requirement;

                    // sanity
                    //if (string.IsNullOrWhiteSpace(column.SchemaName)) continue;
                    if (string.IsNullOrWhiteSpace(column.TableName)) continue;

                    // table schema
                    ITableSchema tableSchema;
                    //var unique = $"{column.SchemaName}|{column.TableName}";
                    var unique = column.TableName;
                    if (tables.ContainsKey(unique))
                    {
                        int idx = tables[unique];
                        tableSchema = _schema.Tables[idx];
                    }
                    else
                    {
                        tableSchema = new XTableSchema() { TableName = column.TableName };
                        //tableSchema.SchemaName = column.SchemaName;
                        tables.Add(unique, _schema.Tables.Count);
                        _schema.Tables.Add(tableSchema);
                    }
                    tableSchema.Columns.Add(column);
                }
            }


            // created time
            _schema.CreatedTime = DateTime.Now;
        }

        // done
        return _schema;
    }

    private string BuildSql(IEnumerable<string> tableNames)
    {
        string sql = @"
                SELECT	schema_name(t.schema_id) as [SchemaName], 
		                t.name as [TableName],  
		                c.name as ColumnName, 
		                c.is_identity as IsPrimaryKey,
		                c.is_nullable as Nullable,
		                c.max_length as MaxLength,
		                ty.name as DataType

                FROM sys.columns c 
                INNER JOIN sys.tables t ON t.object_id = c.object_id 
                INNER JOIN sys.types ty ON c.user_type_id = ty.user_type_id";

        if (tableNames != null && tableNames.Any())
        {
            var pairClauses = tableNames.Select(name => $"(schema_name(t.schema_id) = '{XSchema.GetInfo(name).Schema}' AND t.name = '{XSchema.GetInfo(name).Table}')");
            string whereClause = string.Join(" OR ", pairClauses);
            if (!string.IsNullOrEmpty(whereClause))
                sql += " WHERE " + whereClause;
        }

        return sql;
    }

    #endregion
}
