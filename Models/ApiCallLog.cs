using System.ComponentModel.DataAnnotations;

namespace BuildWorkflowAPI.Models;

public class ApiCallLog
{
    public int Id { get; set; }
    public string Route { get; set; } = "";
    public int? ApiDefinitionId { get; set; }
    public ApiDefinition? ApiDefinition { get; set; }
    public int? ApiKeyId { get; set; }
    public ApiKey? ApiKey { get; set; }
    [MaxLength(200)] public string? KeyName { get; set; }
    public string? RequestPayload { get; set; }
    public string? ResponsePayload { get; set; }
    public int StatusCode { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}