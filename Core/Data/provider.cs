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

namespace Uniya.Core;

/// <summary>The data provider.</summary>
public class XProvider
{
    static Dictionary<string, IReadonlyData> _sources = new Dictionary<string, IReadonlyData>();

    public static IReadonlyData GetData(string source)
    {
        if (_sources.ContainsKey(source))
        {
            return _sources[source];
        }
        return null;
    }
    public static void SetData(string source, IReadonlyData data)
    {
        if (data == null)
        {
            throw new ArgumentNullException(nameof(data));
        }
        if (string.IsNullOrWhiteSpace(source))
        {
            throw new ArgumentNullException(nameof(source));
        }
        if (_sources.ContainsKey(source))
        {
            _sources[source] = data;
        }
        else
        {
            _sources.Add(source, data);
        }
    }
    public static async Task<ITransactedData> LocalDatabase(string source, ILocalDb db)
    {
        // sanity
        if (db == null)
        {
            throw new ArgumentNullException(nameof(db));
        }
        if (string.IsNullOrWhiteSpace(source))
        {
            throw new ArgumentNullException(nameof(source));
        }

        // initialization
        ITransactedData data = null;
        if (!db.IsExist)
        {
            // create new local database
            if (!await db.Create())
            {
                // bad done
                return null;
            }

            // transacted access
            data = db.Data;

            // create SQL script by model
            var xset = new XSet();
            var script = db.GetSqlScript(XSet.Schema);
            await db.RunScript(script);

            // fill start database
            xset.Fill();
            await xset.CommitChanges(data);
        }

        // done
        return data;
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
}
