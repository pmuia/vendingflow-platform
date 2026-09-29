using MachineService.Domain.Entities.MachineModule;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MachineService.Infrastructure.EntityConfigurations.MachineModule;

public class MachineConfiguration : IEntityTypeConfiguration<Machine>
{
    public void Configure(EntityTypeBuilder<Machine> builder)
    {
        builder.HasKey(machine => machine.Id);
        builder.Property(machine => machine.Id).ValueGeneratedNever();
        builder.HasIndex(machine => machine.MachineCode).IsUnique();
        builder.Property(machine => machine.MachineCode).HasMaxLength(32).IsRequired();
        builder.Property(machine => machine.Name).HasMaxLength(120).IsRequired();
        builder.Property(machine => machine.Location).HasMaxLength(200).IsRequired();
        builder.Property(machine => machine.Status).HasConversion<string>().HasMaxLength(32);
    }
}
