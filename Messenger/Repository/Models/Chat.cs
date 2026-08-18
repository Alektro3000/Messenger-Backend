using System.ComponentModel.DataAnnotations.Schema;

namespace Models;

public enum ChatType
{
    Direct = 0,
    Group = 1
}


//Entity to store information about Direct or Group Chat
public class Chat
{
    public long Id { get; set; }
    public required ChatType Type {get; set; }

    public long? DirectUser1Id {get; set; }
    public User? DirectUser1 {get; set; }
    public long? DirectUser2Id {get; set; }
    public User? DirectUser2 {get; set; }

    public string? DisplayName {get; set; }
    public string? AvatarUrl { get; set; }

    [Column(TypeName = "timestamp with time zone")]
    public DateTime CreatedAt { get; set; }

    public ICollection<ChatMember> ChatMembers { get; set; } = [];
    
    public long? LastMessageId { get; set; }
    public Message? LastMessage {get; set; }
}