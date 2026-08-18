using System.ComponentModel.DataAnnotations.Schema;

namespace Models;

public enum MessageType
{
    Text = 0
}

//Entity to store information about Message
public class Message
{
    public long Id { get; set; }
    public int CurrentVersion { get; set; } = 0;
    public long UserId { get; set; }
    public User? User { get; set; }
    
    public long ChatId { get; set; }
    public Chat? Chat { get; set; }

    public MessageType Type {get; set; }
    public string? Text { get; set; }

    [Column(TypeName = "timestamp with time zone")]
    public DateTime SendTime { get; set; }

    [Column(TypeName = "timestamp with time zone")]
    public DateTime? EditTime { get; set; }

    [Column(TypeName = "timestamp with time zone")]
    public DateTime? DeleteTime { get; set; }
}