using System.ComponentModel.DataAnnotations.Schema;

namespace Models;

public class MessageHistory
{
    public long Id { get; set; }
    public long Version { get; set; }
    public string? Text { get; set; }
    
    [Column(TypeName = "timestamp with time zone")]
    public DateTime SendTime { get; set; }
}