using Microsoft.EntityFrameworkCore;
using SupportFlow.Api.Models;

namespace SupportFlow.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketComment> TicketComments => Set<TicketComment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasKey(user => user.Id);
            entity.HasIndex(user => user.Email).IsUnique();
            entity.Property(user => user.FullName).HasMaxLength(120).IsRequired();
            entity.Property(user => user.Email).HasMaxLength(180).IsRequired();
            entity.Property(user => user.PasswordHash).HasMaxLength(500).IsRequired();
            entity.Property(user => user.Role).HasMaxLength(20).IsRequired();
        });

        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.HasKey(ticket => ticket.Id);
            entity.HasIndex(ticket => ticket.Status);
            entity.HasIndex(ticket => ticket.Priority);
            entity.HasIndex(ticket => ticket.CreatedAtUtc);
            entity.Property(ticket => ticket.Title).HasMaxLength(150).IsRequired();
            entity.Property(ticket => ticket.Description).HasMaxLength(4000).IsRequired();
            entity.Property(ticket => ticket.Status).HasConversion<string>().HasMaxLength(30);
            entity.Property(ticket => ticket.Priority).HasConversion<string>().HasMaxLength(20);
            entity.Property(ticket => ticket.Version).IsConcurrencyToken();

            entity.HasOne(ticket => ticket.CreatedByUser)
                .WithMany(user => user.CreatedTickets)
                .HasForeignKey(ticket => ticket.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(ticket => ticket.AssignedToUser)
                .WithMany(user => user.AssignedTickets)
                .HasForeignKey(ticket => ticket.AssignedToUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TicketComment>(entity =>
        {
            entity.HasKey(comment => comment.Id);
            entity.Property(comment => comment.Message).HasMaxLength(2000).IsRequired();

            entity.HasOne(comment => comment.Ticket)
                .WithMany(ticket => ticket.Comments)
                .HasForeignKey(comment => comment.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(comment => comment.Author)
                .WithMany(user => user.Comments)
                .HasForeignKey(comment => comment.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
