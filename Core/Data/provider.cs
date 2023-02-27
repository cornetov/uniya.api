using System;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.NetworkInformation;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Globalization;
using System.Reflection;
using System.Net.Mail;
using System.Diagnostics;
using System.Dynamic;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using System.Text.RegularExpressions;

namespace Uniya.Core;

/// <summary>The data provider.</summary>
public class XProvider
{
    // ------------------------------------------------------------------------------------
    #region ** fields & constructor

    //string _id;
    private ISchema _schema;
    private ILocalDb _local;
    private readonly ITransactedData _main;
    private Dictionary<string, KeyValuePair<IConnection, IReadonlyData>> _dbs = new();

    //private string _entityName;
    //static ObservableCollection<KeyValuePair<string, string>> _names = new ObservableCollection<KeyValuePair<string, string>>();
    //static ObservableCollection<KeyValuePair<string, string>> _names = new ObservableCollection<KeyValuePair<string, string>>();

    /// <summary>
    /// The main database provider.
    /// </summary>
    /// <param name="data"></param>
    protected XProvider(ITransactedData data)
    {
        _main = data;
    }

    #endregion

    // ------------------------------------------------------------------------------------
    #region ** main database

    public IReadonlyData GetData(string database)
    {
        if (_dbs.ContainsKey(database))
        {
            return _dbs[database].Value;
        }
        return null;
    }
    //public void SetData(string database, IReadonlyData data)
    //{
    //    if (string.IsNullOrWhiteSpace(database))
    //    {
    //        throw new ArgumentNullException(nameof(database));
    //    }
    //    if (data == null)
    //    {
    //        throw new ArgumentNullException(nameof(data));
    //    }
    //    if (_dbs.ContainsKey(database))
    //    {
    //        _dbs[database] = data;
    //    }
    //    else
    //    {
    //        _dbs.Add(database, data);
    //    }
    //}

    #endregion

    // ------------------------------------------------------------------------------------
    #region ** object model

    public async Task<IReadOnlyList<string>> GetDatabases(string mask = null)
    {
        // initialization
        if (_dbs.Count == 0)
        {
            foreach (var entity in await _main.Read("Connection"))
            {
                var connection = entity.To<IConnection>();
                _dbs.Add(connection.Name, new KeyValuePair<IConnection, IReadonlyData>(connection, null));
            }
        }

        // empty mask
        if (string.IsNullOrWhiteSpace(mask))
        {
            // all names
            return _dbs.Keys.ToList();
        }

        // mask pattern
        var rx = MaskToRegex(mask);

        // done
        return _dbs.Keys.Where(x => rx.IsMatch(x)).ToList();
    }

    public async Task<IReadOnlyList<string>> GetTables(string database, string mask = null)
    {
        // initialization
        var databases = await GetDatabases(database);
        if (databases.Count == 0)
        {
            foreach (var entity in await _main.Read("Connection"))
            {
                var connection = entity.To<IConnection>();
                _dbs.Add(connection.Name, new KeyValuePair<IConnection, IReadonlyData>(connection, null));
            }
        }

        // empty mask
        if (string.IsNullOrWhiteSpace(mask))
        {
            // all names
            return _dbs.Keys.ToList();
        }

        // mask pattern
        var rx = MaskToRegex(mask);

        // done
        return _dbs.Keys.Where(x => rx.IsMatch(x)).ToList();
    }

    #endregion

    // ------------------------------------------------------------------------------------
    #region ** static object model

    public static async Task<XProvider> LocalDatabase(ILocalDb db)
    {
        // sanity
        if (db == null)
        {
            throw new ArgumentNullException(nameof(db));
        }

        // schema
        ISchema schema = null;

        // exist database?
        if (db.IsExist)
        {
            // TODO: read schema
            //db.
            schema = XSet.Schema;
        }
        else
        {
            // create new local database
            if (!await db.Create())
            {
                // bad done
                return null;
            }

            // create SQL script by model
            var xset = new XSet();
            schema = XSet.Schema;
            var script = db.GetSqlScript(schema);
            await db.RunScript(script);

            // fill start database
            xset.Fill();
            await xset.CommitChanges(db.Data);
        }

        // initialization provider
        XProvider provider = new(db.Data)
        {
            _schema = schema,
            _local = db
        };

        // done
        return provider;
    }

    public static Regex MaskToRegex(string mask)
    {
        var m = "^" + Regex.Escape(mask).Replace("\\*", ".*").Replace("\\?", ".") + "$";
        return new Regex(m, RegexOptions.IgnoreCase);
    }

    static Dictionary<string, Type> _types;

    internal static Type GetConnectorType(string className)
    {
        if (_types == null)
        {
            var baseType = typeof(IReadonlyData);
            _types = new Dictionary<string, Type>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var types = assembly.GetTypes().Where(t => t.IsClass && !t.IsAbstract && baseType.IsAssignableFrom(t));
                    var dic = types.ToDictionary(t => t.Name);
                    foreach (var pair in dic)
                    {
                        var name = pair.Value.FullName.Split(' ')[0];
                        if (!string.IsNullOrWhiteSpace(name) && !_types.ContainsKey(name))
                            _types.Add(name, pair.Value);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }
        if (_types.ContainsKey(className))
        {
            return _types[className];
        }
        return null;
    }

    #endregion
}
