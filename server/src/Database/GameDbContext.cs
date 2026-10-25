using Microsoft.EntityFrameworkCore;
using ArenaServer.Models;

namespace ArenaServer.Database;

public class GameDbContext : DbContext
{
    public DbSet<Player> Players { get; set; }
    public DbSet<Match> Matches { get; set; }
    public DbSet<MatchResult> MatchResults { get; set; }
    public DbSet<Session> Sessions { get; set; }

    public GameDbContext(DbContextOptions<GameDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Player
        modelBuilder.Entity<Player>()
            .HasKey(p => p.Id);
        modelBuilder.Entity<Player>()
            .Property(p => p.Id)
            .ValueGeneratedOnAdd();
        modelBuilder.Entity<Player>()
            .HasIndex(p => p.Username)
            .IsUnique();
        modelBuilder.Entity<Player>()
            .HasMany(p => p.Matches)
            .WithOne(m => m.Player)
            .HasForeignKey(m => m.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);

        // Match
        modelBuilder.Entity<Match>()
            .HasKey(m => m.Id);
        modelBuilder.Entity<Match>()
            .HasMany(m => m.Players)
            .WithMany()
            .UsingEntity(j => j.ToTable("match_players"));

        // MatchResult
        modelBuilder.Entity<MatchResult>()
            .HasKey(mr => mr.Id);
        modelBuilder.Entity<MatchResult>()
            .HasOne(mr => mr.Player)
            .WithMany()
            .HasForeignKey(mr => mr.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<MatchResult>()
            .HasOne(mr => mr.Match)
            .WithMany(m => m.Results)
            .HasForeignKey(mr => mr.MatchId)
            .OnDelete(DeleteBehavior.Cascade);

        // Session
        modelBuilder.Entity<Session>()
            .HasKey(s => s.Id);
        modelBuilder.Entity<Session>()
            .HasOne(s => s.Player)
            .WithMany(p => p.Sessions)
            .HasForeignKey(s => s.PlayerId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Session>()
            .HasIndex(s => s.Token)
            .IsUnique();

        // Default values
        modelBuilder.Entity<Player>()
            .Property(p => p.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        modelBuilder.Entity<Match>()
            .Property(m => m.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        modelBuilder.Entity<Session>()
            .Property(s => s.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
    }
}
