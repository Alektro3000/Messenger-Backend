
//Entity to store Information about User Sessions
using System.ComponentModel.DataAnnotations.Schema;

public class Session
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public required string SecretHash { get; set; }

    [Column(TypeName = "timestamp with time zone")]
    public DateTime ExpireTime { get; set; }

    [Column(TypeName = "timestamp with time zone")]
    public DateTime? RevokedAt { get; set; }

    [Column(TypeName = "timestamp with time zone")]
    public DateTime CreatedAt { get; set; }
    
    [Column(TypeName = "timestamp with time zone")]
    public DateTime LastActivityAt  { get; set; }
}