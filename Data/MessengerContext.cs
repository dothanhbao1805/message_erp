using Microsoft.EntityFrameworkCore;
using messenger.Models;

namespace messenger.Data
{
    public class MessengerContext : DbContext
    {
        public MessengerContext(DbContextOptions<MessengerContext> options) : base(options)
        {
        }

        // DbSets
        public DbSet<Users> Users { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<ConversationMember> ConversationMembers { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<MessageRead> MessageReads { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<UserNotification> UserNotifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ==================== USERS ====================
            modelBuilder.Entity<Users>(entity =>
            {
                entity.ToTable("Users");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();

                // Indexes
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.CreatedAt);

                // Constraints
                entity.Property(e => e.FullName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Password_Hash).IsRequired().HasMaxLength(500);
                entity.Property(e => e.Provider).HasMaxLength(50).HasDefaultValue("local");
                entity.Property(e => e.Role).HasMaxLength(20).HasDefaultValue("User");
                entity.Property(e => e.Status).HasMaxLength(20).HasDefaultValue("Active");
            });

            // ==================== REFRESH TOKENS ====================
            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.ToTable("RefreshTokens");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();

                // Indexes
                entity.HasIndex(e => e.Token).IsUnique();
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.ExpiresAt);

                // Constraints
                entity.Property(e => e.UserId).IsRequired();
                entity.Property(e => e.Token).IsRequired().HasMaxLength(500);
                entity.Property(e => e.ExpiresAt).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.IsRevoked).IsRequired().HasDefaultValue(false);

                // Relationships
                entity.HasOne(e => e.User)
                      .WithMany(u => u.RefreshTokens)
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ==================== CONVERSATIONS ====================
            modelBuilder.Entity<Conversation>(entity =>
            {
                entity.ToTable("Conversations");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();

                // Indexes
                entity.HasIndex(e => e.Type);
                entity.HasIndex(e => e.LastMessageAt);
                entity.HasIndex(e => e.CreatedAt);

                // Constraints
                entity.Property(e => e.Type).IsRequired().HasMaxLength(50).HasDefaultValue("private");
                entity.Property(e => e.Name).HasMaxLength(255);
                entity.Property(e => e.Avatar).HasMaxLength(500);
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UpdatedAt).IsRequired();

                // Relationships
                entity.HasOne(e => e.LastMessage)
                      .WithMany()
                      .HasForeignKey(e => e.LastMessageId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // ==================== CONVERSATION MEMBERS ====================
            modelBuilder.Entity<ConversationMember>(entity =>
            {
                entity.ToTable("ConversationMembers");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();

                // Indexes
                entity.HasIndex(e => new { e.ConversationId, e.UserId }).IsUnique();
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.JoinedAt);

                // Constraints
                entity.Property(e => e.ConversationId).IsRequired();
                entity.Property(e => e.UserId).IsRequired();
                entity.Property(e => e.Role).HasMaxLength(50).HasDefaultValue("member");
                entity.Property(e => e.IsMuted).HasDefaultValue(false);
                entity.Property(e => e.IsPinned).HasDefaultValue(false);
                entity.Property(e => e.JoinedAt).IsRequired();

                // Relationships
                entity.HasOne(e => e.Conversation)
                      .WithMany(c => c.ConversationMembers)
                      .HasForeignKey(e => e.ConversationId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.User)
                      .WithMany(u => u.ConversationMembers)
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.LastSeenMessage)
                      .WithMany()
                      .HasForeignKey(e => e.LastSeenMessageId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // ==================== MESSAGES ====================
            modelBuilder.Entity<Message>(entity =>
            {
                entity.ToTable("Messages");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();

                // Indexes
                entity.HasIndex(e => e.ConversationId);
                entity.HasIndex(e => e.SenderId);
                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => new { e.ConversationId, e.CreatedAt });
                entity.HasIndex(e => e.Status);

                // Constraints
                entity.Property(e => e.ConversationId).IsRequired();
                entity.Property(e => e.SenderId).IsRequired();
                entity.Property(e => e.Type).IsRequired().HasMaxLength(50).HasDefaultValue("text");
                entity.Property(e => e.Content).HasColumnType("text");
                entity.Property(e => e.FileUrl).HasMaxLength(1000);
                entity.Property(e => e.FileName).HasMaxLength(255);
                entity.Property(e => e.Status).HasMaxLength(20).HasDefaultValue("sent");
                entity.Property(e => e.IsEdited).HasDefaultValue(false);
                entity.Property(e => e.IsDeleted).HasDefaultValue(false);
                entity.Property(e => e.CreatedAt).IsRequired();

                // Relationships
                entity.HasOne(e => e.Conversation)
                      .WithMany(c => c.Messages)
                      .HasForeignKey(e => e.ConversationId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Sender)
                      .WithMany(u => u.Messages)
                      .HasForeignKey(e => e.SenderId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.ParentMessage)
                      .WithMany()
                      .HasForeignKey(e => e.ParentMessageId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.ReplyToMessage)
                      .WithMany(m => m.Replies)
                      .HasForeignKey(e => e.ReplyToMessageId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // ==================== MESSAGE READS ====================
            modelBuilder.Entity<MessageRead>(entity =>
            {
                entity.ToTable("MessageReads");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();

                // Indexes
                entity.HasIndex(e => new { e.MessageId, e.UserId }).IsUnique();
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.ReadAt);

                // Constraints
                entity.Property(e => e.MessageId).IsRequired();
                entity.Property(e => e.UserId).IsRequired();
                entity.Property(e => e.ReadAt).IsRequired();

                // Relationships
                entity.HasOne(e => e.Message)
                      .WithMany(m => m.MessageReads)
                      .HasForeignKey(e => e.MessageId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ==================== NOTIFICATIONS ====================
            modelBuilder.Entity<Notification>(entity =>
            {
                entity.ToTable("Notifications");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();

                // Indexes
                entity.HasIndex(e => e.Type);
                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => new { e.EntityType, e.EntityId });

                // Constraints
                entity.Property(e => e.Type).IsRequired().HasMaxLength(50).HasDefaultValue("message");
                entity.Property(e => e.Title).HasMaxLength(255);
                entity.Property(e => e.Content).HasColumnType("text");
                entity.Property(e => e.Url).HasMaxLength(500);
                entity.Property(e => e.EntityType).HasMaxLength(50).HasDefaultValue("message");
                entity.Property(e => e.CreatedAt).IsRequired();
            });

            // ==================== USER NOTIFICATIONS ====================
            modelBuilder.Entity<UserNotification>(entity =>
            {
                entity.ToTable("UserNotifications");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();

                // Indexes
                entity.HasIndex(e => new { e.UserId, e.NotificationId }).IsUnique();
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.IsRead);
                entity.HasIndex(e => new { e.UserId, e.IsRead });

                // Constraints
                entity.Property(e => e.UserId).IsRequired();
                entity.Property(e => e.NotificationId).IsRequired();
                entity.Property(e => e.IsRead).HasDefaultValue(false);

                // Relationships
                entity.HasOne(e => e.User)
                      .WithMany(u => u.UserNotifications)
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Notification)
                      .WithMany(n => n.UserNotifications)
                      .HasForeignKey(e => e.NotificationId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}