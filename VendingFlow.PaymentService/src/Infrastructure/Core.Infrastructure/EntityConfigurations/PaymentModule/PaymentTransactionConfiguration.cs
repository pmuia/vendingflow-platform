using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentService.Domain.Entities.PaymentModule;

namespace PaymentService.Infrastructure.EntityConfigurations.PaymentModule;

public class PaymentTransactionConfiguration : IEntityTypeConfiguration<PaymentTransaction>
{
    public void Configure(EntityTypeBuilder<PaymentTransaction> builder)
    {
        builder.HasKey(payment => payment.PaymentTransactionId);
        builder.Property(payment => payment.PaymentTransactionId).ValueGeneratedNever();
        builder.HasIndex(payment => payment.PaymentTransactionCode).IsUnique();
        builder.HasIndex(payment => new { payment.VendingTransactionId, payment.TransactionType }).IsUnique();
        builder.Property(payment => payment.Amount).HasPrecision(12, 2);
        builder.Property(payment => payment.PaymentTransactionCode).HasMaxLength(64);
        builder.Property(payment => payment.TransactionType).HasConversion<string>().HasMaxLength(32);
        builder.Property(payment => payment.TransactionCode).HasConversion<string>().HasMaxLength(32);
        builder.Property(payment => payment.Status).HasConversion<string>().HasMaxLength(32);
    }
}
