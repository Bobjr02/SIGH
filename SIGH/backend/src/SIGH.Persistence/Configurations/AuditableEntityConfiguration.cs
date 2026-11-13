using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIGH.Domain.Common;

namespace SIGH.Persistence.Configurations;

public abstract class AuditableEntityConfiguration<TEntity> : IEntityTypeConfiguration<TEntity>
    where TEntity : AuditableEntity
{
    public virtual void Configure(EntityTypeBuilder<TEntity> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.CreatedAt).IsRequired();
        // ValueGeneratedNever: o EF sempre envia o valor (a entidade já nasce com false) e não precisa ler
        // de volta após o INSERT. Sem isso, cada inserção em lote vira MERGE ... OUTPUT INSERTED.[IsDeleted].
        builder.Property(e => e.IsDeleted).HasDefaultValue(false).ValueGeneratedNever();
    }
}
