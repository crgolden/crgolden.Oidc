namespace Clarity.Oidc
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    public class RoleClaimConfiguration : IEntityTypeConfiguration<RoleClaim>
    {
        public void Configure(EntityTypeBuilder<RoleClaim> roleClaim)
        {
            roleClaim.ToTable("RoleClaims");
        }
    }
}
