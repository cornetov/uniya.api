/*
 * Recent versions of SQLite allow you to select against PRAGMA results now, which makes this easy:
 * 
 *     SELECT 
 *       m.name as table_name, 
 *       p.name as column_name
 *     FROM 
 *       sqlite_master AS m
 *     JOIN 
 *       pragma_table_info(m.name) AS p
 *     ORDER BY 
 *       m.name, 
 *       p.cid
 *     where p.cid holds the column order of the CREATE TABLE statement, zero-indexed.
 * 
 *     David Garoutte answered this here, but this SQL should execute faster, and columns are ordered by the schema, not alphabetically.
 * 
 *     Note that table_info also contains
 * 
 *     type (the datatype, like integer or text),
 *     notnull (1 if the column has a NOT NULL constraint)
 *     dflt_value (NULL if no default value)
 *     pk (1 if the column is the table's primary key, else 0)
 *     RTFM: https://www.sqlite.org/pragma.html#pragma_table_info
 */

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Reflection;
using System.Diagnostics;

using Microsoft.Data.Sqlite;

using Uniya.Core;

namespace Uniya.Connectors.Sqlite;

/// <summary>
/// Sqlite local database.
/// </summary>
/// <remarks>
/// DataTypes https://www.tutorialspoint.com/sqlite/sqlite_data_types.htm
/// </remarks>
public class SqliteLocal : ILocalDb
{
    // -------------------------------------------------------------------------
    #region ** fields & constructor

    // ** fields
    private SqliteConnector _connector;
    private string _outputFolder;
    private string _name;

    /// <summary>
    /// Create Sqlite local database.
    /// </summary>
    public SqliteLocal()
    {
        _outputFolder = SqlConnector.DataFolder;
    }

    #endregion

    // -------------------------------------------------------------------------
    #region ** ILocalDb support

    /// <summary>Gets exist whether database or no.</summary>
    public bool IsExist
    {
        get
        {
            // directory?
            if (!Directory.Exists(_outputFolder)) return false;

            // names
            var mdfFilename = Name + ".sqlite";
            var dbFileName = Path.Combine(_outputFolder, mdfFilename);

            // database file?
            if (!File.Exists(dbFileName)) return false;

            // create connector
            //string connectionString = String.Format($@"Data Source={dbFileName};");
            var connectionStringBuilder = new SqliteConnectionStringBuilder();
            connectionStringBuilder.DataSource = dbFileName;
            var connectionString = connectionStringBuilder.ConnectionString;
            _connector = new SqliteConnector(connectionString);

            // done
            return true;
        }
    }
    /// <summary>Gets or sets name of database and file.</summary>
    public string Name
    {
        get { return string.IsNullOrWhiteSpace(_name) ? "Uniya" : _name; }
        set { _name = value; }
    }
    /// <summary>Gets design data interface.</summary>
    public ITransactedData Data
    {
        get { return _connector; }
    }

    /// <summary>
    /// Create new database file using database name.
    /// </summary>
    /// <param name="deleteIfExists">Whether delete exist database or no.</param>
    /// <returns><b>true</b> if created, otherwise <b>false</b>.</returns>
    public async Task<bool> Create(bool deleteIfExists = false)
    {
        // initialization
        var dbFileName = Path.Combine(_outputFolder, $"{Name}.sqlite");

        // create data directory if it doesn't already exist.
        if (!Directory.Exists(_outputFolder))
        {
            Directory.CreateDirectory(_outputFolder);
        }

        // is exists?
        if (File.Exists(dbFileName) && deleteIfExists)
        {
            // to delete old data, remove it here and create a new database
            File.Delete(dbFileName);
            await CreateDatabaseAsync(dbFileName);
        }
        else if (!File.Exists(dbFileName))
        {
            // if the database does not already exist, create it.
            await CreateDatabaseAsync(dbFileName);
        }

        // done
        return IsExist;
    }
    /// <summary>
    /// Delete exist database file.
    /// </summary>
    /// <returns><b>true</b> if deleted, otherwise <b>false</b>.</returns>
    public async Task<bool> Delete()
    {
        if (IsExist)
        {
            try
            {
                // names
                var dbFileName = Path.Combine(_outputFolder, $"{Name}.sqlite");
                //var logFileName = Path.Combine(_outputFolder, $"{Name}_log.ldf");

                // delete
                var fi = new FileInfo(dbFileName);
                if (fi.Exists)
                {
                    await fi.DeleteAsync();
                }

                // done
                return true;
            }
            catch { }
        }

        // bad done
        return false;
    }

    /// <summary>
    /// Run SQL script on the database.
    /// </summary>
    /// <param name="script">The SQL script/</param>
    /// <returns>The asynchrony task.</returns>
    public async Task RunScript(string script)
    {
        // sanity
        var regex = new Regex(Environment.NewLine + "GO");
        var commands = regex.Split(script);

        using (var connection = new SqliteConnection(ConnectionString))
        {
            await connection.OpenAsync();
            for (int i = 0; i < commands.Length; i++)
            {
                var sql = commands[i].Trim();
                if (!string.IsNullOrWhiteSpace(sql))
                {
                    using (var command = new SqliteCommand(sql, connection))
                    {
                        try
                        {
                            await command.ExecuteNonQueryAsync();
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"in line: {sql} -- {ex.Message}");
                        }
                    }
                }
            }
        }
    }

    #endregion

    // -------------------------------------------------------------------------------
    #region ** SQL script model (ILocalDb)

    /// <summary>Gets script code of some model.</summary>
    /// <param name="schema">The schema of the model.</param>
    /// <returns>The SQL initialization script.</returns>
    public string GetSqlScript(ISchema schema)
    {
        // initialization
        var sb = new StringBuilder();

        // tables
        foreach (var table in schema.Tables)
        {
            // script split
            if (sb.Length > 0)
            {
                sb.AppendLine().Append("GO").AppendLine();
            }

            // create table
            sb.Append($"CREATE TABLE [{table.Name}] (").AppendLine();

            // primary key and unique indexes
            var key = table.PrimaryKey;
            sb.Append($"[{key}] INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL UNIQUE");

            // columns
            foreach (var column in table.Columns)
            {
                //if ((column.Requirement & XRequirementOptions.PrimaryKey) != 0)
                if (column.Requirement == XRequirementOptions.PrimaryKey)
                {
                    continue;
                }
                sb.Append(',').AppendLine().Append('[').Append(column.Name).Append(']');
                var dataText = GetDataText(column.DataType);
                sb.Append(' ').Append(dataText);
                if ((column.Requirement & XRequirementOptions.NotNull) != 0)
                {
                    sb.Append(' ').Append("NOT NULL");
                }
                if ((column.Requirement & XRequirementOptions.UniqueKey) != 0)
                {
                    sb.Append(' ').Append("UNIQUE");
                }
                if ((column.Requirement & XRequirementOptions.Required) != 0)
                {
                    switch (dataText)
                    {
                        case "TEXT":
                            sb.Append(' ').Append($"CHECK ({column.Name} <> '')");
                            break;
                        case "INTEGER":
                        case "REAL":
                            sb.Append(' ').Append($"CHECK ({column.Name} <> 0)");
                            break;
                    }
                }
            }
            sb.AppendLine().Append(')');

            // unique indexes
            foreach (var index in table.Indexes)
            {
                sb.Append("GO").AppendLine();
                var indexName = $"{index.Name}_{table.Name}";
                var indexOn = new StringBuilder();
                foreach (var column in index.Columns)
                {
                    if (indexOn.Length > 0) indexOn.Append(", ");
                    indexOn.Append('[').Append(column.Name).Append(']');
                }
                sb.Append($"CREATE UNIQUE INDEX [{indexName}]").AppendLine();
                sb.Append($"ON [{table.Name}] ({indexOn})").AppendLine();
            }
        }

        // done
        return sb.ToString();
    }

    static string GetDataText(XDataType type)
    {
        switch (type)
        {
            case XDataType.Binary:
                return "BLOB";
            case XDataType.Boolean:
                return "BOOLEAN";
            case XDataType.Byte:
            case XDataType.Int16:
            case XDataType.Int32:
            case XDataType.Int64:
                return "INTEGER";
            case XDataType.DateTime:
                return "DATETIME";
            case XDataType.Date:
                return "DATE";
            case XDataType.Time:
                return "TIME";
            case XDataType.Decimal:
            case XDataType.Double:
            case XDataType.Currency:
                return "REAL";
        }
        return "TEXT";
    }

    #endregion

    // -------------------------------------------------------------------------
    #region ** object model

    /// <summary>Gets connection string.</summary>
    public string ConnectionString
    {
        get { return (_connector != null) ? _connector.ConnectionString : string.Empty; }
    }

    #endregion

    // -------------------------------------------------------------------------
    #region ** static object model

    public static string GetConnectionString(string dbName)
    {
        // initialization
        var outputFolder = SqlConnector.DataFolder;
        var mdfFilename = dbName + ".sqlite";
        var dbFileName = Path.Combine(outputFolder, mdfFilename);

        // done
        return $@"Data Source=(LocalDB)\mssqllocaldb;AttachDBFileName={dbFileName};Initial Catalog={dbName};Integrated Security=True;";
    }
    public static bool IsExistDatabase(string dbName)
    {
        // initialization
        var outputFolder = SqlConnector.DataFolder;
        var mdfFilename = dbName + ".sqlite";
        var dbFileName = Path.Combine(outputFolder, mdfFilename);

        // exist?
        if (Directory.Exists(outputFolder))
        {
            // done
            return File.Exists(dbFileName);
        }

        // not found
        return false;
    }

    public static async Task CreateDatabaseAsync(string dbName, bool deleteIfExists = false)
    {
        // initialization
        var outputFolder = SqlConnector.DataFolder;
        var mdfFilename = dbName + ".sqlite";
        var dbFileName = Path.Combine(outputFolder, mdfFilename);
        //var logFileName = Path.Combine(outputFolder, String.Format("{0}_log.ldf", dbName));

        // create data directory if it doesn't already exist.
        if (!Directory.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
        }

        // is exists?
        if (File.Exists(dbFileName) && deleteIfExists)
        {
            // to delete old data, remove it here and create a new database
            //if (File.Exists(logFileName)) File.Delete(logFileName);
            File.Delete(dbFileName);
            await CreateDatabaseAsync(dbFileName);
        }
        else if (!File.Exists(dbFileName))
        {
            // if the database does not already exist, create it.
            await CreateDatabaseAsync(dbFileName);
        }
    }

    public static async Task CreateDatabaseAsync(string dbFileName)
    {
        var connectionStringBuilder = new SqliteConnectionStringBuilder();
        connectionStringBuilder.DataSource = dbFileName;
        var connectionString = connectionStringBuilder.ConnectionString;

        //string connectionString = String.Format($@"Data Source={dbFileName};New=True;");

        //SqliteConnection.ChangeDatabase(("MyDatabase.sqlite");
        using (var connection = new SqliteConnection(connectionString))
        {
            //connection.

            //connection.Open();
            await connection.OpenAsync();
            //var result = connection.Query<int>("SELECT @number;", new { number = 789 });
            SqliteCommand cmd = connection.CreateCommand();
            cmd.CommandText = @"SELECT @number;";
            cmd.Parameters.AddWithValue("number", 586);
            var result = await cmd.ExecuteScalarAsync();
            Debug.Assert(Convert.ToInt32(result) == 586);
#if DEBUGx
            SqliteCommand cmd = connection.CreateCommand();
            cmd.CommandText = @"CREATE TABLE [dbTableName]([id] INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL, [note] NVARCHAR(256))";
            await cmd.ExecuteNonQueryAsync();

            cmd.CommandText = @"DELETE TABLE [dbTableName]";
            await cmd.ExecuteNonQueryAsync();
#endif
        }
    }

    public static XEntityCollection Read(string connectionString, string entityName)
    {
        // empty initialization
        var collection = new XEntityCollection();

        // read selected
        using (var connection = new SqliteConnection(connectionString))
        {
            connection.Open();
            //connection.BeginTransaction(IsolationLevel.)
            var cmd = connection.CreateCommand();
            cmd.CommandText = $"SELECT * FROM [dbo].[{entityName}]";
            var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var entity = new XEntity(entityName);
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    entity.Items.Add(reader.GetName(i), reader.GetValue(i));
                }
                collection.Add(entity);
            }
        }

        // done
        return collection;
    }

    public static async Task<XEntityCollection> ReadAsync(string connectionString, string entityName)
    {
        // empty initialization
        var collection = new XEntityCollection();

        // read selected
        using (var connection = new SqliteConnection(connectionString))
        {
            await connection.OpenAsync();
            var cmd = connection.CreateCommand();
            cmd.CommandText = $"SELECT * FROM [dbo].[{entityName}]";
            var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var entity = new XEntity(entityName);
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    entity.Items.Add(reader.GetName(i), reader.GetValue(i));
                }
                collection.Add(entity);
            }
        }

        // done
        return collection;
    }

    public static async Task RunScriptAsync(string connectionString, string script)
    {
        // sainty

        var regex = new Regex(@"\r\nGO");
        var commands = regex.Split(script);

        using (var connection = new SqliteConnection(connectionString))
        {
            await connection.OpenAsync();
            for (int i = 0; i < commands.Length; i++)
            {
                var sql = commands[i].Trim();
                if (!string.IsNullOrWhiteSpace(sql))
                {
                    using (var command = new SqliteCommand(sql, connection))
                    {
                        try
                        {
                            await command.ExecuteNonQueryAsync();
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"in line: {sql} -- {ex.Message}");
                        }
                    }
                }
            }
        }
    }

    public static void RunScript(SqliteConnection connection, string script)
    {
        var regex = new Regex(@"\r\nGO");
        var commands = regex.Split(script);

        for (int i = 0; i < commands.Length; i++)
        {
            var sql = commands[i].Trim();
            if (!string.IsNullOrWhiteSpace(sql))
            {
                using (var command = new SqliteCommand(sql, connection))
                {
                    try
                    {
                        command.ExecuteNonQuery();
                    }
                    catch { }
                }
            }
        }
    }

    public static void InstallScript(SqliteConnection connection, string scriptPath)
    {
        var script = File.ReadAllText(scriptPath);
        RunScript(connection, script);
        var directory = Path.GetDirectoryName(scriptPath);
        foreach (var csvFileName in Directory.GetFiles(directory, "*.csv"))
        {
        }
    }

    #endregion
}
