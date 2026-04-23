using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VTOS.Infrastructure.Persistence.Configurations;

namespace VTOS.Infrastructure.Persistence;

internal static class VtosModelConfigurationRegistry
{
    internal static void Apply(ModelBuilder modelBuilder)
    {
        var registeredConfigurations = new HashSet<Type>();

        Apply(modelBuilder, new AccountRequestConfiguration(), registeredConfigurations);
        Apply(modelBuilder, new BodygramMeasurementRecordConfiguration(), registeredConfigurations);
        Apply(modelBuilder, new BodygramScanLogConfiguration(), registeredConfigurations);
        Apply(modelBuilder, new BodygramScanRecordConfiguration(), registeredConfigurations);
        Apply(modelBuilder, new CategoryConfiguration(), registeredConfigurations);
        Apply(modelBuilder, new ChatMessageConfiguration(), registeredConfigurations);
        Apply(modelBuilder, new ChildProfileConfiguration(), registeredConfigurations);
        Apply(modelBuilder, new ClassGroupConfiguration(), registeredConfigurations);
        Apply(modelBuilder, new ContractConfiguration(), registeredConfigurations);
        Apply(modelBuilder, new ContractItemConfiguration(), registeredConfigurations);
        Apply(modelBuilder, new EmailVerificationConfiguration(), registeredConfigurations);
        Apply(modelBuilder, new FeedbackConfiguration(), registeredConfigurations);
        Apply(modelBuilder, new InvoiceConfiguration(), registeredConfigurations);
        Apply(modelBuilder, new NotificationLogConfiguration(), registeredConfigurations);
        Apply(modelBuilder, new OrderConfiguration(), registeredConfigurations);
        Apply(modelBuilder, new OrderItemConfiguration(), registeredConfigurations);
        Apply(modelBuilder, new OutfitCategoryConfiguration(), registeredConfigurations);
        Apply(modelBuilder, new OutfitConfiguration(), registeredConfigurations);
        Apply(modelBuilder, new ParentAddressConfiguration(), registeredConfigurations);
        Apply(modelBuilder, new ParentBankAccountConfiguration(), registeredConfigurations);
        Apply(modelBuilder, new ParentProfileConfiguration(), registeredConfigurations);
        Apply(modelBuilder, new PaymentTransactionConfiguration(), registeredConfigurations);
        Apply(modelBuilder, new PayoutRecordConfiguration(), registeredConfigurations);
        Apply(modelBuilder, new ProductVariantConfiguration(), registeredConfigurations);
        Apply(modelBuilder, new ProviderCatalogItemConfiguration(), registeredConfigurations);
        Apply(modelBuilder, new ProviderConfiguration(), registeredConfigurations);
        Apply(modelBuilder, new ProviderManagerConfiguration(), registeredConfigurations);
        Apply(modelBuilder, new ProviderRatingConfiguration(), registeredConfigurations);
        Apply(modelBuilder, new RefundConfiguration(), registeredConfigurations);
        Apply(modelBuilder, new RoleConfiguration(), registeredConfigurations);
        Apply(modelBuilder, new SchoolConfiguration(), registeredConfigurations);
        Apply(modelBuilder, new SchoolManagerConfiguration(), registeredConfigurations);
        Apply(modelBuilder, new SemesterPublicationConfiguration(), registeredConfigurations);
        Apply(modelBuilder, new SemesterPublicationOutfitConfiguration(), registeredConfigurations);
        Apply(modelBuilder, new SemesterPublicationProviderConfiguration(), registeredConfigurations);
        Apply(modelBuilder, new SizeChartConfiguration(), registeredConfigurations);
        Apply(modelBuilder, new SizeChartDetailConfiguration(), registeredConfigurations);
        Apply(modelBuilder, new SizeChartMeasurementConfiguration(), registeredConfigurations);
        Apply(modelBuilder, new StudentDataImportConfiguration(), registeredConfigurations);
        Apply(modelBuilder, new TeacherReportConfiguration(), registeredConfigurations);
        Apply(modelBuilder, new TryOnHistoryConfiguration(), registeredConfigurations);
        Apply(modelBuilder, new UserConfiguration(), registeredConfigurations);
        Apply(modelBuilder, new WalletConfiguration(), registeredConfigurations);
        Apply(modelBuilder, new WalletWithdrawalRequestConfiguration(), registeredConfigurations);

        ValidateAllConfigurationsAreRegistered(registeredConfigurations);
    }

    private static void Apply<TEntity>(
        ModelBuilder modelBuilder,
        IEntityTypeConfiguration<TEntity> configuration,
        ISet<Type> registeredConfigurations)
        where TEntity : class
    {
        modelBuilder.ApplyConfiguration(configuration);
        registeredConfigurations.Add(configuration.GetType());
    }

    private static void ValidateAllConfigurationsAreRegistered(ISet<Type> registeredConfigurations)
    {
        var discoveredConfigurations = typeof(VTOSDbContext).Assembly
            .GetTypes()
            .Where(type =>
                type is { IsAbstract: false, IsInterface: false } &&
                type.GetInterfaces().Any(@interface =>
                    @interface.IsGenericType &&
                    @interface.GetGenericTypeDefinition() == typeof(IEntityTypeConfiguration<>)))
            .ToHashSet();

        var missingConfigurations = discoveredConfigurations
            .Except(registeredConfigurations)
            .OrderBy(type => type.FullName)
            .ToArray();

        if (missingConfigurations.Length == 0)
        {
            return;
        }

        throw new InvalidOperationException(
            "EF model configuration registry is incomplete. Register these configurations explicitly: " +
            string.Join(", ", missingConfigurations.Select(type => type.Name)));
    }
}
