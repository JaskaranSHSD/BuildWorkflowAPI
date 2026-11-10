using System.Text.Json;
using BuildWorkflowAPI.Data;
using BuildWorkflowAPI.Models;
using BuildWorkflowAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BuildWorkflowAPI.Controllers;

[ApiController]
[Route("api/{*route}")]
public class ApiGatewayController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ApiExecutor _exec;
    private readonly ILogger<ApiGatewayController> _logger;
    public ApiGatewayController(AppDbContext db, ApiExecutor exec, ILogger<ApiGatewayController> logger) { _db = db; _exec = exec; _logger = logger; }

    [HttpGet, HttpPost, HttpPut, HttpDelete]
    public async Task<IActionResult> Invoke(string route, CancellationToken ct)
    {
        var api = await _db.ApiDefinitions.Include(a => a.Connection).Include(a => a.Parameters).Include(a => a.ApiKeys)
            .FirstOrDefaultAsync(a => a.Route == route, ct);
        if (api is null) return NotFound(new { error = "API not found" });

        // API key validation
        var key = Request.Headers["X-API-Key"].FirstOrDefault() ?? Request.Query["api_key"].FirstOrDefault();
        var validKeys = await _db.ApiKeys.Where(k => k.IsActive && (k.ApiDefinitionId == null || k.ApiDefinitionId == api.Id)).ToListAsync(ct);
        if (validKeys.Count == 0)
            return Unauthorized(new { error = "No API keys configured for this API." });
        if (string.IsNullOrEmpty(key) || !validKeys.Any(k => k.Key == key))
            return Unauthorized(new { error = "Invalid or missing API key." });
        var matchedKey = validKeys.FirstOrDefault(k => k.Key == key);

        // gather input
        var input = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var q in Request.Query)
            if (!q.Key.Equals("api_key", StringComparison.OrdinalIgnoreCase))
                input[q.Key] = q.Value.ToString();

        if (Request.ContentLength > 0 && Request.ContentType?.Contains("json") == true)
        {
            using var doc = await JsonDocument.ParseAsync(Request.Body, cancellationToken: ct);
            if (doc.RootElement.ValueKind == JsonValueKind.Object)
                foreach (var prop in doc.RootElement.EnumerateObject())
                    input[prop.Name] = prop.Value.Clone();
        }

        var requestJson = TrySerialize(input);
        try
        {
            var res = await _exec.ExecuteAsync(api, input);
            var responseObj = new { resultSets = res.ResultSets, outputParameters = res.OutputParameters };
            var responseJson = TrySerialize(responseObj);
            await LogCallAsync(api.Id, matchedKey, route, requestJson, responseJson, 200, ct);
            return Ok(responseObj);
        }
        catch (Exception ex)
        {
            // Do not leak internal/database error details to callers.
            _logger.LogError(ex, "API execution failed for route {Route}", route);
            var responseObj = new { error = "An error occurred while executing the API." };
            await LogCallAsync(api.Id, matchedKey, route, requestJson, TrySerialize(responseObj), 400, ct);
            return BadRequest(responseObj);
        }
    }

    private static string TrySerialize(object value)
    {
        try
        {
            var json = JsonSerializer.Serialize(value);
            return json.Length > 16384 ? json[..16384] + "...(truncated)" : json;
        }
        catch
        {
            return "{}";
        }
    }

    private async Task LogCallAsync(int apiId, ApiKey? key, string route, string requestJson, string responseJson, int statusCode, CancellationToken ct)
    {
        try
        {
            _db.ApiCallLogs.Add(new ApiCallLog
            {
                ApiDefinitionId = apiId,
                ApiKeyId = key?.Id,
                KeyName = key?.Name,
                Route = route,
                RequestPayload = requestJson,
                ResponsePayload = responseJson,
                StatusCode = statusCode,
                Timestamp = DateTime.UtcNow
            });
            await _db.SaveChangesAsync(ct);
        }
        catch
        {
            // Logging failures must not affect API responses.
        }
    }
}