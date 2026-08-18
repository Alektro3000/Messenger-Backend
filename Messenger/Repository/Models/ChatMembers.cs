using System.ComponentModel.DataAnnotations.Schema;

namespace Models;

//Entity to store information about chat members
public class ChatMember
{
    public long UserId { get; set; }
    public long ChatId { get; set; }

    public long? LastReadMessageId { get; set; }
    public Message? LastReadMessage {get; set; }

    public User? User { get; set; }
    public Chat? Chat { get; set; }
}
