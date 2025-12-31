using Microsoft.Data.SqlClient;
using ReportManager.Shared.Dto;

namespace ReportAdmin.Core.Db;

public static class DbIntrospector
{
    public sealed record ViewColumn(string Name, string SqlType);

    public static async Task<List<ViewColumn>> GetViewColumnsAsync(string connectionString, string schema, string viewName, CancellationToken ct = default)
    {
        const string sql = @"
SELECT c.name, t.name
FROM sys.views v
JOIN sys.schemas s ON s.schema_id = v.schema_id
JOIN sys.columns c ON c.object_id = v.object_id
JOIN sys.types t ON t.user_type_id = c.user_type_id
WHERE s.name = @schema AND v.name = @view
ORDER BY c.column_id;";
        var list = new List<ViewColumn>();

        using var con = new SqlConnection(connectionString);
        await con.OpenAsync(ct);

        using var cmd = con.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@schema", schema);
        cmd.Parameters.AddWithValue("@view", viewName);

        using var rdr = await cmd.ExecuteReaderAsync(ct);
        while (await rdr.ReadAsync(ct))
            list.Add(new ViewColumn(rdr.GetString(0), rdr.GetString(1)));

        return list;
    }

    public static ReportColumnType MapSqlType(string sqlType)
    {
        sqlType = sqlType.ToLowerInvariant();
        return sqlType switch
        {
            "int" => ReportColumnType.Integer,
            "bigint" => ReportColumnType.Long,
            "smallint" => ReportColumnType.Integer,
            "tinyint" => ReportColumnType.Integer,
            "decimal" => ReportColumnType.Decimal,
            "numeric" => ReportColumnType.Decimal,
            "money" => ReportColumnType.Decimal,
            "smallmoney" => ReportColumnType.Decimal,
            "float" => ReportColumnType.Double,
            "real" => ReportColumnType.Double,
            "bit" => ReportColumnType.Boolean,
            "uniqueidentifier" => ReportColumnType.Guid,
            "date" => ReportColumnType.Date,
            "datetime" => ReportColumnType.DateTime,
            "datetime2" => ReportColumnType.DateTime,
            "smalldatetime" => ReportColumnType.DateTime,
            _ => ReportColumnType.String
        };
    }
}
