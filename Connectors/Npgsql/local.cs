using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Reflection;
using System.Diagnostics;

using Npgsql;

using Uniya.Core;

namespace Uniya.Connectors.Npgsql;

/// <summary>Postgre SQL local database.</summary>
/// <remarks>https://zetcode.com/csharp/postgresql/</remarks>
internal class NpgsqlLocal : ILocalDb
{
    // -------------------------------------------------------------------------
    #region ** fields & constructor

    // ** fields
    private NpgsqlConnector _connector;
    private string _outputFolder;
    private string _name;

    /// <summary>
    /// Create Microsoft SQL local database.
    /// </summary>
    public NpgsqlLocal()
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
            var mdfFilename = Name + ".mdf";
            var dbFileName = Path.Combine(_outputFolder, mdfFilename);

            // database file?
            if (!File.Exists(dbFileName)) return false;

            // create connector
            var connectionString = $@"Data Source=(LocalDB)\mssqllocaldb;AttachDBFileName={dbFileName};Initial Catalog={Name};Integrated Security=True;";
            _connector = new NpgsqlConnector(connectionString);

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
    /// <param name="deleteIfExists">Whether delete exist databse or no.</param>
    /// <returns><b>true</b> if created, otherwise <b>false</b>.</returns>
    public async Task<bool> Create(bool deleteIfExists = false)
    {
        // initialization
        var dbFileName = Path.Combine(_outputFolder, $"{Name}.mdf");
        var logFileName = Path.Combine(_outputFolder, $"{Name}_log.ldf");

        //var mdfFilename = Name + ".mdf";
        //var dbFileName = Path.Combine(_outputFolder, mdfFilename);
        //var logFileName = Path.Combine(_outputFolder, String.Format("{0}_log.ldf", Name));

        // create data directory if it doesn't already exist.
        if (!Directory.Exists(_outputFolder))
        {
            Directory.CreateDirectory(_outputFolder);
        }

        // is exists?
        if (File.Exists(dbFileName) && deleteIfExists)
        {
            // to delete old data, remove it here and create a new database
            if (File.Exists(logFileName)) File.Delete(logFileName);
            File.Delete(dbFileName);
            await CreateDatabaseAsync(Name, dbFileName);
        }
        else if (!File.Exists(dbFileName))
        {
            // if the database does not already exist, create it.
            await CreateDatabaseAsync(Name, dbFileName);
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
                // detach
                await DetachDatabaseAsync(ConnectionString, Name);

                // names
                var dbFileName = Path.Combine(_outputFolder, $"{Name}.mdf");
                var logFileName = Path.Combine(_outputFolder, $"{Name}_log.ldf");

                // delete
                if (File.Exists(logFileName)) File.Delete(logFileName);
                File.Delete(dbFileName);

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

        using (var connection = new NpgsqlConnection(ConnectionString))
        {
            await connection.OpenAsync();
            for (int i = 0; i < commands.Length; i++)
            {
                var sql = commands[i].Trim();
                if (!string.IsNullOrWhiteSpace(sql))
                {
                    using (var command = new NpgsqlCommand(sql, connection))
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
        return string.Empty;
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
        var mdfFilename = dbName + ".mdf";
        var dbFileName = Path.Combine(outputFolder, mdfFilename);

        // done
        return $@"Data Source=(LocalDB)\mssqllocaldb;AttachDBFileName={dbFileName};Initial Catalog={dbName};Integrated Security=True;";
    }
    public static bool IsExistDatabase(string dbName)
    {
        // initialization
        var outputFolder = SqlConnector.DataFolder;
        var mdfFilename = dbName + ".mdf";
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

    public static async Task<bool> CreateDatabaseAsync(string dbName, bool deleteIfExists = false)
    {
        // initialization
        var outputFolder = SqlConnector.DataFolder;
        var mdfFilename = dbName + ".mdf";
        var dbFileName = Path.Combine(outputFolder, mdfFilename);
        var logFileName = Path.Combine(outputFolder, String.Format("{0}_log.ldf", dbName));

        // create data directory if it doesn't already exist.
        if (!Directory.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
        }

        // is exists?
        if (File.Exists(dbFileName) && deleteIfExists)
        {
            // to delete old data, remove it here and create a new database
            if (File.Exists(logFileName)) File.Delete(logFileName);
            File.Delete(dbFileName);
            return await CreateDatabaseAsync(dbName, dbFileName);
        }
        else if (!File.Exists(dbFileName))
        {
            // if the database does not already exist, create it.
            return await CreateDatabaseAsync(dbName, dbFileName);
        }

        // done
        return false;
    }

    public static NpgsqlConnection GetDatabase(string dbName, bool deleteIfExists = false)
    {
        bool exist;
        return GetDatabase(dbName, deleteIfExists, out exist);
    }
    public static NpgsqlConnection GetDatabase(string dbName, bool deleteIfExists, out bool exist)
    {
        try
        {
            exist = false;
            string outputFolder = SqlConnector.DataFolder;
            string mdfFilename = dbName + ".mdf";
            string dbFileName = Path.Combine(outputFolder, mdfFilename);
            string logFileName = Path.Combine(outputFolder, String.Format("{0}_log.ldf", dbName));

            // create data directory if it doesn't already exist.
            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }

            // is exists?
            if (File.Exists(dbFileName) && deleteIfExists)
            {
                // to delete old data, remove it here and create a new database
                if (File.Exists(logFileName)) File.Delete(logFileName);
                File.Delete(dbFileName);
                CreateDatabase(dbName, dbFileName);
            }
            else if (!File.Exists(dbFileName))
            {
                // if the database does not already exist, create it.
                CreateDatabase(dbName, dbFileName);
            }
            else
            {
                exist = true;
            }

            // open database
            string connectionString = String.Format(@"Data Source=(LocalDB)\mssqllocaldb;AttachDBFileName={1};Initial Catalog={0};Integrated Security=True;", dbName, dbFileName);
            NpgsqlConnection connection = new(connectionString);
            //connection.Open();

            // done
            return connection;
        }
        catch
        {
            throw;
        }
    }

    public static bool CreateDatabase(string dbName, string dbFileName)
    {
        try
        {
            string connectionString = String.Format(@"Data Source=(LocalDB)\mssqllocaldb;Initial Catalog=master;Integrated Security=True");
            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                NpgsqlCommand cmd = connection.CreateCommand();

                DetachDatabase(dbName);

                cmd.CommandText = $"CREATE DATABASE {dbName} ON (NAME = N'{dbName}', FILENAME = '{dbFileName}')";
                cmd.ExecuteNonQuery();
            }

            // done
            return File.Exists(dbFileName);
        }
        catch
        {
            throw;
        }
    }
    public static async Task<bool> CreateDatabaseAsync(string dbName, string dbFileName)
    {
        string connectionString = String.Format(@"Data Source=(LocalDB)\mssqllocaldb;Initial Catalog=master;Integrated Security=True");
        using (var connection = new NpgsqlConnection(connectionString))
        {
            await connection.OpenAsync();
            NpgsqlCommand cmd = connection.CreateCommand();


            DetachDatabase(dbName);

            cmd.CommandText = $"CREATE DATABASE {dbName} ON (NAME = N'{dbName}', FILENAME = '{dbFileName}')";
            await cmd.ExecuteNonQueryAsync();
        }

        // done
        return File.Exists(dbFileName);
    }
    public static async Task<bool> DetachDatabaseAsync(string connectionString, string dbName)
    {
        try
        {
            using (var connection = new NpgsqlConnection(connectionString))
            {
                await connection.OpenAsync();

                NpgsqlCommand cmd = connection.CreateCommand();
                cmd.CommandText = String.Format("exec sp_detach_db '{0}'", dbName);
                await cmd.ExecuteNonQueryAsync();

                return true;
            }
        }
        catch
        {
            return false;
        }
    }


    public static bool DetachDatabase(string dbName)
    {
        try
        {
            string connectionString = String.Format(@"Data Source=(LocalDB)\mssqllocaldb;Initial Catalog=master;Integrated Security=True");
            using (var connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();
                NpgsqlCommand cmd = connection.CreateCommand();
                cmd.CommandText = String.Format("exec sp_detach_db '{0}'", dbName);
                cmd.ExecuteNonQuery();

                return true;
            }
        }
        catch
        {
            return false;
        }
    }

    public static XEntityCollection Read(string connectionString, string entityName)
    {
        // empty initialization
        var collection = new XEntityCollection();

        // read selected
        using (var connection = new NpgsqlConnection(connectionString))
        {
            connection.Open();
            var cmd = connection.CreateCommand();
            cmd.CommandText = $"SELECT * FROM [{entityName}]";
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
        using (var connection = new NpgsqlConnection(connectionString))
        {
            await connection.OpenAsync();
            var cmd = connection.CreateCommand();
            cmd.CommandText = $"SELECT * FROM [{entityName}]";
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
        // sanity

        var regex = new Regex(@"\r\nGO");
        var commands = regex.Split(script);

        using (var connection = new NpgsqlConnection(connectionString))
        {
            await connection.OpenAsync();
            for (int i = 0; i < commands.Length; i++)
            {
                var sql = commands[i].Trim();
                if (!string.IsNullOrWhiteSpace(sql))
                {
                    using (var command = new NpgsqlCommand(sql, connection))
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

    public static void RunScript(NpgsqlConnection connection, string script)
    {
        var regex = new Regex(@"\r\nGO");
        var commands = regex.Split(script);

        for (int i = 0; i < commands.Length; i++)
        {
            var sql = commands[i].Trim();
            if (!string.IsNullOrWhiteSpace(sql))
            {
                using (var command = new NpgsqlCommand(sql, connection))
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

    public static void InstallScript(NpgsqlConnection connection, string scriptPath)
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
