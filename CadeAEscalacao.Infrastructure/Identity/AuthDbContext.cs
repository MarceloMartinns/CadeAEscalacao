using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CadeAEscalacao.Infrastructure.Identity;

/// <summary>
/// Login tables (ADR-014), apart from the domain's <see cref="Persistence.AppDbContext"/>: same database,
/// own migrations (<c>dotnet ef ... --context AuthDbContext</c>). Without Identity roles: the roles
/// belong to each group (ADR-016).
/// </summary>
/// <remarks>
/// Mapped here and not with <c>IEntityTypeConfiguration</c> classes, because the
/// <see cref="Persistence.AppDbContext"/> applies every configuration of this assembly.
/// </remarks>
public sealed class AuthDbContext(DbContextOptions<AuthDbContext> options)
    : IdentityUserContext<AppUser, Guid>(options)
{
    /// <summary>Separate history table, so each context only sees its own migrations.</summary>
    internal const string MigrationsHistoryTable = "__ef_auth_migrations_history";

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<AppUser>(user =>
        {
            user.ToTable("users");
            user.HasIndex(appUser => appUser.NormalizedUserName).HasDatabaseName("ix_users_normalized_user_name");
            user.HasIndex(appUser => appUser.NormalizedEmail).HasDatabaseName("ix_users_normalized_email");

            // One account per player. The foreign key to "players" (a table of the other context)
            // is written by hand in the migration.
            user.HasIndex(appUser => appUser.PlayerId).IsUnique();
        });

        builder.Entity<IdentityUserClaim<Guid>>().ToTable("user_claims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("user_logins");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("user_tokens");

        builder.Entity<RefreshToken>(token =>
        {
            token.ToTable("refresh_tokens");
            token.HasKey(refreshToken => refreshToken.Id);
            token.Property(refreshToken => refreshToken.Id).ValueGeneratedNever();
            token.Property(refreshToken => refreshToken.TokenHash).HasMaxLength(44);
            token.HasIndex(refreshToken => refreshToken.TokenHash).IsUnique();
            token.HasOne<AppUser>().WithMany().HasForeignKey(refreshToken => refreshToken.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
