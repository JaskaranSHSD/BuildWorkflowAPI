using System.Data;
using System.Data.Common;
using System.Text.Json;
using BuildWorkflowAPI.Models;
using Microsoft.Data.SqlClient;
using MySqlConnector;
using Npgsql;

namespace BuildWorkflowAPI.Services;

public class ApiExecutor
{
    public async Task<ExecutionResult> ExecuteAsync(ApiDefinition api, Dictionary<string, object?> input)
    {
        var result = new ExecutionResult();
        await using var conn = CreateConnection(api.Connection!);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = api.CommandText;
        cmd.CommandType = api.CommandType == Models.CommandType.StoredProcedure
            ? System.Data.CommandType.StoredProcedure
            : System.Data.CommandType.Text;

        // add params
        var paramByName = new Dictionary<string, DbParameter>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in api.Parameters)
        {
            var dbp = cmd.CreateParameter();
            dbp.ParameterName = NormalizeName(p.Name, api.Connection!.Provider);
            if (p.IsOutput) dbp.Direction = ParameterDirection.Output;
            else dbp.Direction = ParameterDirection.Input;
            dbp.DbType = MapDbType(p.DbType);
            if (!p.IsOutput)
            {
                if (input.TryGetValue(p.Name, out var v) && v is not null)
                    dbp.Value = ConvertValue(v, p.DbType);
                else if (!string.IsNullOrEmpty(p.DefaultValue))
                    dbp.Value = ConvertValue(p.DefaultValue, p.DbType);
                else if (!p.IsRequired)
                    dbp.Value = DBNull.Value;
                else
                    throw new ArgumentException($"Missing required parameter '{p.Name}'.");
            }
            cmd.Parameters.Add(dbp);
            paramByName[p.Name] = dbp;
        }

        // execute reader
        await using (var reader = await cmd.ExecuteReaderAsync())
        {
            do
            {
                var table = new List<Dictionary<string, object?>>();
                var cols = new List<string>();
                for (int i = 0; i < reader.FieldCount; i++) cols.Add(reader.GetName(i));
                while (await reader.ReadAsync())
                {
                    var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                    for (int i = 0; i < reader.FieldCount; i++)
                        row[cols[i]] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    table.Add(row);
                }
                result.ResultSets.Add(table);
            } while (await reader.NextResultAsync());
        }

        // output params
        foreach (var p in api.Parameters.Where(x => x.IsOutput))
        {
            var dbp = paramByName[p.Name];
            result.OutputParameters[p.Name] = dbp.Value is DBNull ? null : dbp.Value;
        }

        return result;
    }

    private static string NormalizeName(string name, DbProvider provider) =>
        provider == DbProvider.Postgres ? name : ("@" + name.TrimStart('@'));

    // For postgres stored proc params use positional? Npgsql supports named. ok.

    private static DbConnection CreateConnection(ApiConnection c) => c.Provider switch
    {
        DbProvider.MsSql => new SqlConnection(c.ConnectionString),
        DbProvider.MySql => new MySqlConnection(c.ConnectionString),
        DbProvider.Postgres => new NpgsqlConnection(c.ConnectionString),
        _ => throw new NotSupportedException()
    };

    private static DbType MapDbType(string t) => t.ToLowerInvariant() switch
    {
        "int" or "integer" => DbType.Int32,
        "long" or "bigint" => DbType.Int64,
        "double" or "float" => DbType.Double,
        "decimal" => DbType.Decimal,
        "bool" or "boolean" => DbType.Boolean,
        "datetime" or "date" => DbType.DateTime,
        "guid" => DbType.Guid,
        _ => DbType.String
    };

    private static object ConvertValue(object v, string dbType)
    {
        if (v is JsonElement je)
        {
            return dbType.ToLowerInvariant() switch
            {
                "int" or "integer" => je.GetInt32(),
                "long" or "bigint" => je.GetInt64(),
                "double" or "float" => je.GetDouble(),
                "decimal" => je.GetDecimal(),
                "bool" or "boolean" => je.GetBoolean(),
                "datetime" or "date" => je.GetDateTime(),
                "guid" => je.GetGuid(),
                _ => je.ToString()
            };
        }
        var s = v?.ToString() ?? "";
        return dbType.ToLowerInvariant() switch
        {
            "int" or "integer" => int.Parse(s),
            "long" or "bigint" => long.Parse(s),
            "double" or "float" => double.Parse(s),
            "decimal" => decimal.Parse(s),
            "bool" or "boolean" => bool.Parse(s),
            "datetime" or "date" => DateTime.Parse(s),
            "guid" => Guid.Parse(s),
            _ => s
        };
    }
}

public class ExecutionResult
{
    public List<List<Dictionary<string, object?>>> ResultSets { get; set; } = new();
    public Dictionary<string, object?> OutputParameters { get; set; } = new();
}