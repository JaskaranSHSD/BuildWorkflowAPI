using System.ComponentModel.DataAnnotations;

using System.ComponentModel.DataAnnotations.Schema;

namespace BuildWorkflowAPI.Models;

[Table("AppUsers")]
public class AppUser
{
    public int Id { get; set; }
    [Required, MaxLength(50)] public string Username { get; set; } = "";
    [Required, MaxLength(200)] public string Email { get; set; } = "";
    [Required] public string PasswordHash { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}