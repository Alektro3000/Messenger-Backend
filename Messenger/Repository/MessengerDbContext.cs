
using Microsoft.EntityFrameworkCore;
using Models;

namespace Repository;

public class MessengerDbContext : DbContext
{
    public DbSet<Chat> Chats { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<ChatMember> ChatMembers { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<MessageHistory> MessageHistories { get; set; }
    public DbSet<Session> Sessions { get; set; }

    public MessengerDbContext(DbContextOptions<MessengerDbContext> options)
        : base(options)
    {

    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasIndex(x => x.Username)
            .IsUnique()
            .HasDatabaseName("UniqueIndex_Users_UserName");

        modelBuilder.Entity<ChatMember>()
            .HasKey(x => new { x.ChatId, x.UserId });
        
        modelBuilder.Entity<ChatMember>()
            .HasOne(x => x.Chat)
            .WithMany(x => x.ChatMembers)
            .HasForeignKey(x => x.ChatId);

        modelBuilder.Entity<ChatMember>()
            .HasOne(x => x.User)
            .WithMany(x => x.Chats)
            .HasForeignKey(x => x.UserId);
            
        modelBuilder.Entity<Message>()
            .HasIndex(m => new { m.ChatId, m.Id });

        modelBuilder.Entity<Chat>()
            .HasIndex(c => new { c.DirectUser1Id, c.DirectUser2Id })
            .IsUnique();

        modelBuilder.Entity<Chat>()
            .HasOne(c => c.LastMessage)
            .WithMany()
            .HasForeignKey(c=>c.LastMessageId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MessageHistory>()
            .HasKey(x => new { x.Id, x.Version });
        
    }
}