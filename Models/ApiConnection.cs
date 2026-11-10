using System.ComponentModel.DataAnnotations;

namespace BuildWorkflowAPI.Models;

public class ApiConnection
{
    public int Id { get; set; }
    [Required, MaxLength(100)] public string Name { get; set; } = "";
    public DbProvider Provider { get; set; }
    [Required] public string ConnectionString { get; set; } = "";
    public ICollection<ApiDefinition> ApiDefinitions { get; set; } = new List<ApiDefinition>();
}

public class ApiParameter
{
    public int Id { get; set; }
    public int ApiDefinitionId { get; set; }
    [Required, MaxLength(100)] public string Name { get; set; } = "";
    [MaxLength(50)] public string DbType { get; set; } = "string";
    public bool IsOutput { get; set; }
    public bool IsRequired { get; set; } = true;
    public string? DefaultValue { get; set; }
    public ApiDefinition? ApiDefinition { get; set; }
}

public class ApiDefinition
{
    public int Id { get; set; }
    [Required, MaxLength(100)] public string Name { get; set; } = "";
    [Required, MaxLength(100)] public string Route { get; set; } = "";
    [MaxLength(10)] public string HttpMethod { get; set; } = "POST";
    public int ConnectionId { get; set; }
    public ApiConnection? Connection { get; set; }
    public CommandType CommandType { get; set; } = CommandType.StoredProcedure;
    [Required] public string CommandText { get; set; } = "";
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<ApiParameter> Parameters { get; set; } = new List<ApiParameter>();
    public ICollection<ApiKey> ApiKeys { get; set; } = new List<ApiKey>();
}

public class ApiKey
{
    public int Id { get; set; }
    public int? ApiDefinitionId { get; set; }
    public ApiDefinition? ApiDefinition { get; set; }
    [Required, MaxLength(200)] public string Name { get; set; } = "";
    [Required] public string Key { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}