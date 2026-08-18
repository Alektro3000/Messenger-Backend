using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;

namespace Models;

//Entity to store information about User of messenger 
public class User
{
    public long Id { get; set; }
    public required string Username { get; set; }
    public required string DisplayName { get; set; }
    public string? Name { get; set; }
    public string? Surname { get; set; }
    public string? Bio { get; set; }
    public string? PasswordHash { get; set; }
    public string? AvatarUrl { get; set; }

    [Column(TypeName = "timestamp with time zone")]
    public DateTime CreatedAt { get; set; }
    [Column(TypeName = "timestamp with time zone")]
    public DateTime LastSeenAt { get; set; }
    
    public List<ChatMember> Chats { get; set; } = [];

 
}