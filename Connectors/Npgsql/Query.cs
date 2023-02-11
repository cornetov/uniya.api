using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Uniya.Core;

namespace Uniya.Connectors.MsSql;

public class MsSqlQuery : XQuery
{
    private string _sql;

    public MsSqlQuery(string entityName, string sql)
        : base(entityName)
    {
        _sql = sql;
    }

    /// <summary>Gets SQL code for the query expression.</summary>
    /// <returns>The query SQL code.</returns>
    public override string ToSql()
    {
        return _sql;
    }
}
