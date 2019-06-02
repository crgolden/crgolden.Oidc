namespace crgolden.Oidc
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading.Tasks;
    using IdentityServer4.EntityFramework.Entities;
    using IdentityServer4.EntityFramework.Extensions;
    using IdentityServer4.EntityFramework.Interfaces;
    using IdentityServer4.EntityFramework.Options;
    using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore;

    [ExcludeFromCodeCoverage]
    public class OidcDbContext : IdentityDbContext<User, Role, Guid, UserClaim, UserRole, UserLogin, RoleClaim, UserToken>,
        IConfigurationDbContext,
        IPersistedGrantDbContext
    {
        /// <inheritdoc />
        /// <param name="options"></param>
        public OidcDbContext(DbContextOptions<OidcDbContext> options) : base(options) { }

        /// <inheritdoc />
        public virtual DbSet<Client> Clients { get; set; }

        /// <inheritdoc />
        public virtual DbSet<IdentityResource> IdentityResources { get; set; }

        /// <inheritdoc />
        public virtual DbSet<ApiResource> ApiResources { get; set; }

        /// <inheritdoc />
        public virtual DbSet<PersistedGrant> PersistedGrants { get; set; }

        /// <inheritdoc />
        public virtual DbSet<DeviceFlowCodes> DeviceFlowCodes { get; set; }

        /// <inheritdoc cref="IConfigurationDbContext.SaveChangesAsync" />
        /// <inheritdoc cref="IPersistedGrantDbContext.SaveChangesAsync" />
        public virtual Task<int> SaveChangesAsync()
        {
            return base.SaveChangesAsync();
        }

        /// <inheritdoc />
        /// <param name="modelBuilder"></param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfiguration(new RoleConfiguration());
            modelBuilder.ApplyConfiguration(new RoleClaimConfiguration());
            modelBuilder.ApplyConfiguration(new UserConfiguration());
            modelBuilder.ApplyConfiguration(new UserClaimConfiguration());
            modelBuilder.ApplyConfiguration(new UserLoginConfiguration());
            modelBuilder.ApplyConfiguration(new UserRoleConfiguration());
            modelBuilder.ApplyConfiguration(new UserTokenConfiguration());

            var configurationStoreOptions = new ConfigurationStoreOptions();
            modelBuilder.ConfigureClientContext(configurationStoreOptions);
            modelBuilder.ConfigureResourcesContext(configurationStoreOptions);

            var operationalStoreOptions = new OperationalStoreOptions();
            modelBuilder.ConfigurePersistedGrantContext(operationalStoreOptions);
        }
    }
}
