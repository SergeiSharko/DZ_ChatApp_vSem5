using Microsoft.EntityFrameworkCore;

namespace DZ_ChatApp_vSem5.Models
{
    // Строка подкоючения с сайта connectionstring
    //для SQLServer "Server=localHost;Database=chatUDP;User Id=root;Password=12345;"
    //для MySQL "Server=localHost;Database=chatUDP;Uid=root;Pwd=12345;"
    //для Postgres "Host=localHost;Username=postgres;Password=example;Database=chatUDP;"
    public class ChatUdpContext : DbContext
    {
        public virtual DbSet<User> Users { get; set; }
        public virtual DbSet<Message> Messages { get; set; }
        public ChatUdpContext() { }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string conStrg = "Server=localHost;Database=chatUDP;Uid=root;Pwd=12345;";
            optionsBuilder.LogTo(Console.WriteLine)
                          .UseLazyLoadingProxies()
                          .UseMySql(conStrg, ServerVersion.AutoDetect(conStrg));
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Message>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("message_pkey");
                entity.ToTable("Message");

                entity.Property(x => x.Id).HasColumnName("Id");
                entity.Property(x => x.Text).HasColumnName("Text");
                entity.Property(x => x.FromUserId).HasColumnName("MessageFromUser");
                entity.Property(x => x.ToUserId).HasColumnName("MessageToUser");
                entity.Property(x => x.isReceived).HasColumnName("isReceived");

                // создание свзяи многие ко многим 
                entity.HasOne(d => d.FromUser).WithMany(p => p.MessagesFromUser).HasForeignKey(e => e.FromUserId).HasConstraintName("message_FromUserId_fkey");
                entity.HasOne(d => d.ToUser).WithMany(p => p.MessagesToUser).HasForeignKey(e => e.ToUserId).HasConstraintName("message_ToUserId_fkey");

            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("users_pkey");
                entity.ToTable("Users");

                entity.Property(x => x.Id).HasColumnName("Id");
                entity.Property(x => x.Name).HasMaxLength(255).HasColumnName("Name");
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}
