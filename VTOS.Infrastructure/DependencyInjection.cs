using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VTOS.Application.Abstractions;
using VTOS.Application.Common.Settings;
using VTOS.Application.Features.Admin.Queries;
using VTOS.Application.Features.Admin.Commands;
using VTOS.Application.Features.Account.Commands;
using VTOS.Application.Features.Auth.Commands;
using VTOS.Application.Features.Auth.Queries;
using VTOS.Application.Features.Public.Queries;
using VTOS.Application.Features.TryOn.Commands.GuestTryOn;
using VTOS.Application.Features.Users.Commands;
using VTOS.Application.Features.Users.Queries;
using VTOS.Infrastructure.ExternalServices.Google;
using VTOS.Infrastructure.ExternalServices.ImageStorage;
using VTOS.Infrastructure.ExternalServices.Turnstile;
using VTOS.Infrastructure.ExternalServices.TryOn;
using VTOS.Infrastructure.Bodygram;
using VTOS.Infrastructure.Persistence;
using VTOS.Infrastructure.Services;
using AutoMapper;
using VTOS.Application.Features.Children.Commands;
using VTOS.Application.Features.Children.Mappings;
using VTOS.Application.Features.Children.Queries;
using VTOS.Infrastructure.ExternalServices.PayOS;
using VTOS.Application.Features.Orders.Commands;
using VTOS.Application.Features.Orders.Queries;
using VTOS.Application.Features.Payments.Commands;
using VTOS.Application.Features.Teachers.Commands;
using VTOS.Application.Features.Teachers.Queries;
using VTOS.Application.Features.SupportTickets;
using VTOS.Application.Features.Schools.Services;
namespace VTOS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Add In-Memory Cache (reduces DB load for public endpoints)
        services.AddMemoryCache();

        // Add DbContext — supports SQL Server (dev) and PostgreSQL (prod)
        var dbProvider = configuration.GetValue<string>("DatabaseProvider") ?? "SqlServer";

        // Npgsql 6+ strict UTC: DateTime with Kind=Unspecified is rejected for 'timestamp with time zone'.
        // Enable legacy behavior so Npgsql treats Unspecified as UTC (prevents ArgumentException on user-supplied dates).
        if (dbProvider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
        {
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        }

        services.AddDbContext<VTOSDbContext>(options =>
        {
            if (dbProvider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
            {
                options.UseNpgsql(
                    configuration.GetConnectionString("DefaultConnection"),
                    b => b.MigrationsAssembly(typeof(VTOSDbContext).Assembly.FullName));
            }
            else
            {
                options.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection"),
                    b => b.MigrationsAssembly(typeof(VTOSDbContext).Assembly.FullName));
            }
        });

        // Register DbContext as IApplicationDbContext
        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<VTOSDbContext>());

        // Register JWT Settings
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        // Register Email Settings
        services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));

        // Register TryOn Settings (UC-60)
        services.Configure<VirtualTryOnSettings>(configuration.GetSection(VirtualTryOnSettings.SectionName));
        services.Configure<TryOnSettings>(configuration.GetSection(TryOnSettings.SectionName));
        services.Configure<GeminiTryOnSettings>(configuration.GetSection(GeminiTryOnSettings.SectionName));
        services.Configure<TryOnProviderSettings>(configuration.GetSection(TryOnProviderSettings.SectionName));
        services.Configure<MinioSettings>(configuration.GetSection(MinioSettings.SectionName));
        services.Configure<TryOnImageSecuritySettings>(configuration.GetSection(TryOnImageSecuritySettings.SectionName));


        // Register PayOS Settings
        services.Configure<PayOSSettings>(configuration.GetSection(PayOSSettings.SectionName));

        // Register Payment Settings
        services.Configure<PaymentSettings>(configuration.GetSection(PaymentSettings.SectionName));

        // Register Cloudflare Turnstile Settings
        services.Configure<TurnstileSettings>(configuration.GetSection(TurnstileSettings.SectionName));

        // Register Frontend Settings
        services.Configure<FrontendSettings>(configuration.GetSection(FrontendSettings.SectionName));

  // Bodygram Service Settings
        services.Configure<BodygramSettings>(configuration.GetSection(BodygramSettings.SectionName));

        // Register Services
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // Register TryOn Services with HttpClient (UC-60)
        // Register both concrete services
        services.AddHttpClient<VirtualTryOnService>();
        services.AddHttpClient<GeminiTryOnService>();
        // Register selector as the IVirtualTryOnService implementation
        services.AddScoped<IVirtualTryOnService>(sp =>
        {
            var selector = new TryOnServiceSelector(
                sp.GetRequiredService<VirtualTryOnService>(),
                sp.GetRequiredService<GeminiTryOnService>(),
                sp.GetRequiredService<IOptions<TryOnProviderSettings>>(),
                sp.GetRequiredService<ILogger<TryOnServiceSelector>>()
            );
            return selector;
        });
        services.AddSingleton<MinioImageService>();
        services.AddSingleton<IImageUploadService>(sp => sp.GetRequiredService<MinioImageService>());
        services.AddSingleton<IPrivateImageStorageService>(sp => sp.GetRequiredService<MinioImageService>());
        services.AddScoped<ITryOnImageAccessService, TryOnImageAccessService>();
        services.AddSingleton<IImageWatermarkService, ImageWatermarkService>();
        services.AddHttpClient<IImageDownloadService, ImageDownloadService>();

        //Register PayOS Service
        services.AddHttpClient<IPayOSService, PayOSService>();

  //Register Bodygram Service
        services.AddHttpClient<IBodygramService, BodygramService>();

        // Register Handlers
        services.AddScoped<IRegisterCommandHandler, RegisterCommandHandler>();
        services.AddScoped<ILoginQueryHandler, LoginQueryHandler>();
        services.AddScoped<IVerifyEmailCommandHandler, VerifyEmailCommandHandler>();
        services.AddScoped<ResendOTPCommandHandler>();
        services.AddScoped<VerifyPhoneCommandHandler>();
        services.AddScoped<ForgotPasswordCommandHandler>();
        services.AddScoped<ResetPasswordCommandHandler>();
        services.AddScoped<RequestChangePasswordOTPCommandHandler>();
        services.AddScoped<ChangePasswordCommandHandler>();
        services.AddScoped<IUpdateAccountEmailCommandHandler, UpdateAccountEmailCommandHandler>();

        // Two-Factor Authentication
        services.AddScoped<ITotpService, VTOS.Infrastructure.ExternalServices.TwoFactor.TotpService>();
        services.AddScoped<VTOS.Application.Features.Auth.Commands.TwoFactor.ISetup2FACommandHandler,
            VTOS.Application.Features.Auth.Commands.TwoFactor.Setup2FACommandHandler>();
        services.AddScoped<VTOS.Application.Features.Auth.Commands.TwoFactor.IConfirm2FACommandHandler,
            VTOS.Application.Features.Auth.Commands.TwoFactor.Confirm2FACommandHandler>();
        services.AddScoped<VTOS.Application.Features.Auth.Commands.TwoFactor.IDisable2FACommandHandler,
            VTOS.Application.Features.Auth.Commands.TwoFactor.Disable2FACommandHandler>();
        services.AddScoped<VTOS.Application.Features.Auth.Commands.TwoFactor.IVerify2FACommandHandler,
            VTOS.Application.Features.Auth.Commands.TwoFactor.Verify2FACommandHandler>();

        // Google OAuth
        services.AddHttpClient<IGoogleTokenValidator, GoogleTokenValidator>();
        services.AddScoped<VTOS.Application.Features.Auth.Commands.IGoogleLoginCommandHandler,
            VTOS.Application.Features.Auth.Commands.GoogleLoginCommandHandler>();

        // Cloudflare Turnstile
        services.AddHttpClient<ITurnstileVerifier, TurnstileVerifier>();

        // Register Validators
        services.AddValidatorsFromAssemblyContaining<RegisterCommandHandler>();

        // Register Mappers
        services.AddAutoMapper(cfg => { }, typeof(ChildProfileMappingProfile).Assembly);

        //View User List & Feedbacks
        services.AddScoped<IGetAllUsersQueryHandler, GetAllUsersQueryHandler>();
        services.AddScoped<IGetAllFeedbacksQueryHandler, GetAllFeedbacksQueryHandler>();

        //Approve-Suspend User & Remove Feedback
        services.AddScoped<IApproveUserCommandHandler, ApproveUserCommandHandler>();
        services.AddScoped<ISuspendUserCommandHandler, SuspendUserCommandHandler>();
        services.AddScoped<IRemoveFeedbackCommandHandler, RemoveFeedbackCommandHandler>();

        //Parent Personal Infor
        services.AddScoped<IGetProfileQueryHandler, GetProfileQueryHandler>();
        services.AddScoped<IUpdateProfileCommandHandler, UpdateProfileCommandHandler>();
        services.AddScoped<IUpdateAvatarCommandHandler, UpdateAvatarCommandHandler>();
        services.AddScoped<ISubmitVerificationCommandHandler, SubmitVerificationCommandHandler>();
        services.AddScoped<IGetParentAddressesQueryHandler, GetParentAddressesQueryHandler>();
        services.AddScoped<IUpsertParentAddressCommandHandler, UpsertParentAddressCommandHandler>();
        services.AddScoped<IDeleteParentAddressCommandHandler, DeleteParentAddressCommandHandler>();
        services.AddScoped<ISetDefaultParentAddressCommandHandler, SetDefaultParentAddressCommandHandler>();

        // Parent - Children Management
        services.AddScoped<GetMyChildrenQueryHandler>();
        services.AddScoped<FindChildrenCommandHandler>();


        //Child Infor
        services.AddScoped<IGetMyChildProfileQueryHandler, GetMyChildProfileQueryHandler>();
        services.AddScoped<IGetChildProfileQueryHandler, GetChildProfileQueryHandler>();
        services.AddScoped<IUpdateChildProfileCommandHandler, UpdateChildProfileCommandHandler>();
        services.AddScoped<IUpdateChildAvatarCommandHandler, UpdateChildAvatarCommandHandler>();

        // Public Module Handlers (UC 3.3.2, 3.3.3, 3.3.4, 3.3.5)
        services.AddScoped<GetSchoolsQueryHandler>();
        services.AddScoped<Application.Features.Public.Queries.GetCategoriesQueryHandler>();
        services.AddScoped<Application.Features.Admin.Queries.GetCategoriesQueryHandler>();
        services.AddScoped<GetOutfitDetailQueryHandler>();
        services.AddScoped<GetSchoolDetailQueryHandler>();
        services.AddScoped<GetUniformListQueryHandler>();
        services.AddScoped<PublicSearchQueryHandler>();
        services.AddScoped<GetUniformWarehouseQueryHandler>();
        services.AddScoped<GetSchoolSemesterCatalogQueryHandler>();
        services.AddScoped<GetAllSchoolSemesterCatalogsQueryHandler>();
        services.AddScoped<GetProvidersForPublicationOutfitQueryHandler>();
        services.AddScoped<GetProviderPublicProfileQueryHandler>();
        services.AddScoped<GetProviderRatingsQueryHandler>();
        services.AddScoped<GetProviderRankingQueryHandler>();

        // TryOn Module Handlers (UC-60)
        services.AddScoped<IGuestTryOnCommandHandler, GuestTryOnCommandHandler>();
        services.AddScoped<VTOS.Application.Features.TryOn.Queries.IGetParentTryOnHistoryQueryHandler,
            VTOS.Application.Features.TryOn.Queries.GetParentTryOnHistoryQueryHandler>();

        // Orders Module Handlers (Checkout, Cancel, Track Status, History)
        services.AddScoped<ICheckoutCommandHandler, CheckoutCommandHandler>();
        services.AddScoped<ICancelOrderCommandHandler, CancelOrderCommandHandler>();
        services.AddScoped<IPaymentWebhookHandler, PaymentWebhookHandler>();
        services.AddScoped<IGetOrderStatusQueryHandler, GetOrderStatusQueryHandler>();
        services.AddScoped<IGetOrderHistoryQueryHandler, GetOrderHistoryQueryHandler>();
        services.AddScoped<IGetOrderDetailForFeedbackQueryHandler, GetOrderDetailForFeedbackQueryHandler>();
        services.AddScoped<IRetryPaymentCommandHandler, RetryPaymentCommandHandler>();
        services.AddScoped<ICancelPaymentTransactionCommandHandler, CancelPaymentTransactionCommandHandler>();
        services.AddScoped<ICreateDirectOrderCommandHandler, CreateDirectOrderCommandHandler>();
        services.AddScoped<ICancelDirectOrderCommandHandler, CancelDirectOrderCommandHandler>();
        services.AddScoped<ISubmitProviderRatingCommandHandler, SubmitProviderRatingCommandHandler>();
        services.AddScoped<IGetMyDirectOrdersQueryHandler, GetMyDirectOrdersQueryHandler>();
        services.AddScoped<IGetMyDirectOrderDetailQueryHandler, GetMyDirectOrderDetailQueryHandler>();
        services.AddScoped<IConfirmDirectOrderDeliveryCommandHandler, ConfirmDirectOrderDeliveryCommandHandler>();
        services.AddScoped<IOrderPaymentResolutionService, OrderPaymentResolutionService>();
        services.AddScoped<IProviderPayoutService, ProviderPayoutService>();

        // Background Jobs
        services.AddHostedService<BackgroundJobs.StaleOrderCleanupService>();
        services.AddHostedService<BackgroundJobs.AutoConfirmDeliveryJob>();
        services.AddHostedService<BackgroundJobs.AutoPayoutJob>();
        services.AddHostedService<BackgroundJobs.PaymentDeadlineReminderJob>();
        services.AddHostedService<BackgroundJobs.AdminNotificationDigestJob>();
        services.AddHostedService<BackgroundJobs.ParentTryOnJobWorker>();
        services.AddScoped<Application.Features.Notifications.INotificationBroadcaster, Hubs.SignalRNotificationBroadcaster>();
        services.AddScoped<Application.Features.Admin.Commands.IUserStatusBroadcaster, Hubs.SignalRUserStatusBroadcaster>();

        // Phase 04: Admin UI Revamp
        services.AddScoped<VTOS.Application.Features.Admin.Queries.IGetAdminCashFlowQueryHandler,
            VTOS.Application.Features.Admin.Queries.GetAdminCashFlowQueryHandler>();
        services.AddScoped<VTOS.Application.Features.Admin.Queries.IGetAllTransactionsQueryHandler,
            VTOS.Application.Features.Admin.Queries.GetAllTransactionsQueryHandler>();
        services.AddScoped<VTOS.Application.Features.Admin.Queries.IGetAllSupportTicketsQueryHandler,
            VTOS.Application.Features.Admin.Queries.GetAllSupportTicketsQueryHandler>();
        services.AddScoped<VTOS.Application.Features.Admin.Commands.IAdminInterventionCommandHandler,
            VTOS.Application.Features.Admin.Commands.AdminInterventionCommandHandler>();

        // Refund Module Handler
        services.AddScoped<VTOS.Application.Features.Schools.Commands.IApproveRefundCommandHandler,
            VTOS.Application.Features.Schools.Commands.ApproveRefundCommandHandler>();

        // School Module Handlers (UC-42, UC-45, UC-46, UC-49, UC-50)
        services.AddScoped<VTOS.Application.Features.Schools.Queries.IGetSchoolProfileQueryHandler,
            VTOS.Application.Features.Schools.Queries.GetSchoolProfileQueryHandler>();
        services.AddScoped<VTOS.Application.Features.Schools.Commands.IUpdateSchoolProfileCommandHandler,
            VTOS.Application.Features.Schools.Commands.UpdateSchoolProfileCommandHandler>();
        services.AddScoped<VTOS.Application.Features.Schools.Queries.IGetSchoolOrdersQueryHandler,
            VTOS.Application.Features.Schools.Queries.GetSchoolOrdersQueryHandler>();
        services.AddScoped<VTOS.Application.Features.Schools.Queries.IGetSalesReportQueryHandler,
            VTOS.Application.Features.Schools.Queries.GetSalesReportQueryHandler>();
        services.AddScoped<VTOS.Application.Features.Schools.Queries.IGetFeedbackReportQueryHandler,
            VTOS.Application.Features.Schools.Queries.GetFeedbackReportQueryHandler>();
        services.AddScoped<VTOS.Application.Features.Schools.Queries.IGetSchoolRefundsQueryHandler,
            VTOS.Application.Features.Schools.Queries.GetSchoolRefundsQueryHandler>();

        // School Module - UC-43: Import Student Data
        services.AddScoped<IStudentCodeGenerator, StudentCodeGenerator>();
        services.AddScoped<VTOS.Application.Features.Schools.Commands.IImportStudentDataCommandHandler,
            VTOS.Application.Features.Schools.Commands.ImportStudentDataCommandHandler>();

        // School Module - Get School Students
        services.AddScoped<VTOS.Application.Features.Schools.Queries.IGetSchoolStudentsQueryHandler,
            VTOS.Application.Features.Schools.Queries.GetSchoolStudentsQueryHandler>();
        services.AddScoped<VTOS.Application.Features.Schools.Queries.IGetSchoolClassesOverviewQueryHandler,
            VTOS.Application.Features.Schools.Queries.GetSchoolClassesOverviewQueryHandler>();
        services.AddScoped<VTOS.Application.Features.Schools.Queries.IGetSchoolClassDetailQueryHandler,
            VTOS.Application.Features.Schools.Queries.GetSchoolClassDetailQueryHandler>();
        services.AddScoped<VTOS.Application.Features.Schools.Queries.IGetTeacherClassesOverviewQueryHandler,
            VTOS.Application.Features.Schools.Queries.GetTeacherClassesOverviewQueryHandler>();
        services.AddScoped<VTOS.Application.Features.Schools.Queries.IGetTeacherClassDetailQueryHandler,
            VTOS.Application.Features.Schools.Queries.GetTeacherClassDetailQueryHandler>();
        services.AddScoped<IGetTeacherDashboardQueryHandler, GetTeacherDashboardQueryHandler>();
        services.AddScoped<IGetTeacherReportsQueryHandler, GetTeacherReportsQueryHandler>();
        services.AddScoped<IGetSchoolTeacherReportsQueryHandler, GetSchoolTeacherReportsQueryHandler>();
        services.AddScoped<IGetTeacherReminderCandidatesQueryHandler, GetTeacherReminderCandidatesQueryHandler>();
        services.AddScoped<IGetTeacherClassOrderCoverageQueryHandler, GetTeacherClassOrderCoverageQueryHandler>();
        services.AddScoped<IGetTeacherClassFeedbackQueryHandler, GetTeacherClassFeedbackQueryHandler>();
        services.AddScoped<ISubmitTeacherReportCommandHandler, SubmitTeacherReportCommandHandler>();
        services.AddScoped<IReviewTeacherReportCommandHandler, ReviewTeacherReportCommandHandler>();
        services.AddScoped<ISendTeacherReminderCommandHandler, SendTeacherReminderCommandHandler>();

        // School Module - Student CRUD
        services.AddScoped<VTOS.Application.Features.Schools.Commands.ICreateStudentCommandHandler,
            VTOS.Application.Features.Schools.Commands.CreateStudentCommandHandler>();
        services.AddScoped<VTOS.Application.Features.Schools.Queries.IGetStudentByIdQueryHandler,
            VTOS.Application.Features.Schools.Queries.GetStudentByIdQueryHandler>();
        services.AddScoped<VTOS.Application.Features.Schools.Commands.IUpdateStudentCommandHandler,
            VTOS.Application.Features.Schools.Commands.UpdateStudentCommandHandler>();
        services.AddScoped<VTOS.Application.Features.Schools.Commands.IDeleteStudentCommandHandler,
            VTOS.Application.Features.Schools.Commands.DeleteStudentCommandHandler>();

        // School Module - Grades + Import History
        services.AddScoped<VTOS.Application.Features.Schools.Queries.IGetSchoolGradesQueryHandler,
            VTOS.Application.Features.Schools.Queries.GetSchoolGradesQueryHandler>();
        services.AddScoped<VTOS.Application.Features.Schools.Queries.IGetImportHistoryQueryHandler,
            VTOS.Application.Features.Schools.Queries.GetImportHistoryQueryHandler>();
        services.AddScoped<VTOS.Application.Features.Schools.Queries.IGetImportStatusQueryHandler,
            VTOS.Application.Features.Schools.Queries.GetImportStatusQueryHandler>();

        // School Module - Outfit CRUD
        services.AddScoped<VTOS.Application.Features.Schools.Queries.IGetSchoolOutfitsQueryHandler,
            VTOS.Application.Features.Schools.Queries.GetSchoolOutfitsQueryHandler>();
        services.AddScoped<VTOS.Application.Features.Schools.Commands.ICreateOutfitCommandHandler,
            VTOS.Application.Features.Schools.Commands.CreateOutfitCommandHandler>();
        services.AddScoped<VTOS.Application.Features.Schools.Commands.IUpdateOutfitCommandHandler,
            VTOS.Application.Features.Schools.Commands.UpdateOutfitCommandHandler>();
        services.AddScoped<VTOS.Application.Features.Schools.Commands.IDeleteOutfitCommandHandler,
            VTOS.Application.Features.Schools.Commands.DeleteOutfitCommandHandler>();

        // School Module - Variant (Size) CRUD
        services.AddScoped<VTOS.Application.Features.Schools.Queries.IGetOutfitVariantsQueryHandler,
            VTOS.Application.Features.Schools.Queries.GetOutfitVariantsQueryHandler>();
        services.AddScoped<VTOS.Application.Features.Schools.Commands.ICreateVariantCommandHandler,
            VTOS.Application.Features.Schools.Commands.CreateVariantCommandHandler>();
        services.AddScoped<VTOS.Application.Features.Schools.Commands.IUpdateVariantCommandHandler,
            VTOS.Application.Features.Schools.Commands.UpdateVariantCommandHandler>();
        services.AddScoped<VTOS.Application.Features.Schools.Commands.IDeleteVariantCommandHandler,
            VTOS.Application.Features.Schools.Commands.DeleteVariantCommandHandler>();

        services.AddScoped<VTOS.Application.Features.Schools.Commands.ICreateSemesterPublicationCommandHandler,
            VTOS.Application.Features.Schools.CreateSemesterPublicationCommandHandler>();
        services.AddScoped<VTOS.Application.Features.Schools.Commands.IUpdateSemesterPublicationCommandHandler,
            VTOS.Application.Features.Schools.UpdateSemesterPublicationCommandHandler>();
        services.AddScoped<VTOS.Application.Features.Schools.Commands.IDeleteDraftPublicationCommandHandler,
            VTOS.Application.Features.Schools.DeleteDraftPublicationCommandHandler>();
        services.AddScoped<VTOS.Application.Features.Schools.Commands.IPublishSemesterPublicationCommandHandler,
            VTOS.Application.Features.Schools.PublishSemesterPublicationCommandHandler>();
        services.AddScoped<VTOS.Application.Features.Schools.Commands.ICloseSemesterPublicationCommandHandler,
            VTOS.Application.Features.Schools.CloseSemesterPublicationCommandHandler>();
        services.AddScoped<VTOS.Application.Features.Schools.Commands.IAddOutfitsToPublicationCommandHandler,
            VTOS.Application.Features.Schools.AddOutfitsToPublicationCommandHandler>();
        services.AddScoped<VTOS.Application.Features.Schools.Commands.IRemoveOutfitFromPublicationCommandHandler,
            VTOS.Application.Features.Schools.RemoveOutfitFromPublicationCommandHandler>();
        services.AddScoped<VTOS.Application.Features.Schools.Commands.IApproveProviderCommandHandler,
            VTOS.Application.Features.Schools.ApproveProviderCommandHandler>();
        services.AddScoped<VTOS.Application.Features.Schools.Commands.ISuspendProviderCommandHandler,
            VTOS.Application.Features.Schools.SuspendProviderCommandHandler>();

        services.AddScoped<VTOS.Application.Features.Schools.Queries.IGetProductionSupportTicketsQueryHandler,
            VTOS.Application.Features.Schools.Queries.GetProductionSupportTicketsQueryHandler>();
        services.AddScoped<VTOS.Application.Features.Schools.Queries.IGetSemesterPublicationsQueryHandler,
            VTOS.Application.Features.Schools.GetSemesterPublicationsQueryHandler>();
        services.AddScoped<VTOS.Application.Features.Schools.Queries.IGetSemesterPublicationDetailQueryHandler,
            VTOS.Application.Features.Schools.GetSemesterPublicationDetailQueryHandler>();
        services.AddScoped<VTOS.Application.Features.Schools.Queries.IGetContractedOutfitSuggestionsQueryHandler,
            VTOS.Application.Features.Schools.GetContractedOutfitSuggestionsQueryHandler>();
        services.AddScoped<VTOS.Application.Features.Schools.Queries.IGetContractedProviderSuggestionsQueryHandler,
            VTOS.Application.Features.Schools.GetContractedProviderSuggestionsQueryHandler>();
        // School Module - Provider Listing
        services.AddScoped<VTOS.Application.Features.Schools.Queries.IGetProvidersQueryHandler,
            VTOS.Application.Features.Schools.Queries.GetProvidersQueryHandler>();

        // Admin Module - Approve Withdrawal
        services.AddScoped<VTOS.Application.Features.Admin.Commands.IApproveWithdrawalCommandHandler,
            VTOS.Application.Features.Admin.Commands.ApproveWithdrawalCommandHandler>();

        // Admin Module - Reject Withdrawal
        services.AddScoped<VTOS.Application.Features.Admin.Commands.IRejectWithdrawalCommandHandler,
            VTOS.Application.Features.Admin.Commands.RejectWithdrawalCommandHandler>();

        // Parent Module - Bank Account
        services.AddScoped<VTOS.Application.Features.Users.Commands.IAddParentBankAccountCommandHandler,
            VTOS.Application.Features.Users.Commands.AddParentBankAccountCommandHandler>();

        // Admin Module - Get Withdrawal Requests
        services.AddScoped<VTOS.Application.Features.Admin.Queries.IGetWithdrawalRequestsQueryHandler,
            VTOS.Application.Features.Admin.Queries.GetWithdrawalRequestsQueryHandler>();

        // Provider Module - Withdrawal Request
        services.AddScoped<VTOS.Application.Features.Providers.Commands.ICreateProviderWithdrawalRequestCommandHandler,
            VTOS.Application.Features.Providers.Commands.CreateProviderWithdrawalRequestCommandHandler>();
        services.AddScoped<VTOS.Application.Features.Providers.Queries.IGetProviderWithdrawalRequestsQueryHandler,
            VTOS.Application.Features.Providers.Queries.GetProviderWithdrawalRequestsQueryHandler>();

        // Provider Module - Profile
        services.AddScoped<VTOS.Application.Features.Providers.Queries.IGetProviderProfileQueryHandler,
            VTOS.Application.Features.Providers.Queries.GetProviderProfileQueryHandler>();
        services.AddScoped<VTOS.Application.Features.Providers.Commands.ISubmitProviderRatingCommandHandler,
            VTOS.Application.Features.Providers.Commands.SubmitProviderRatingCommandHandler>();
        services.AddScoped<VTOS.Application.Features.Providers.Queries.IGetProviderRatingsQueryHandler,
            VTOS.Application.Features.Providers.Queries.GetProviderRatingsQueryHandler>();
        services.AddScoped<VTOS.Application.Features.Providers.Queries.IGetProviderRankingQueryHandler,
            VTOS.Application.Features.Providers.Queries.GetProviderRankingQueryHandler>();
        services.AddScoped<VTOS.Application.Features.Providers.Commands.IUpdateProviderProfileCommandHandler,
            VTOS.Application.Features.Providers.Commands.UpdateProviderProfileCommandHandler>();

        services.AddScoped<VTOS.Application.Features.Providers.Queries.IGetProviderIncomingOrdersQueryHandler,
            VTOS.Application.Features.Providers.Queries.GetProviderIncomingOrdersQueryHandler>();
        services.AddScoped<VTOS.Application.Features.Providers.Queries.IGetProviderDirectOrderDetailQueryHandler,
            VTOS.Application.Features.Providers.Queries.GetProviderDirectOrderDetailQueryHandler>();
        services.AddScoped<VTOS.Application.Features.Providers.Queries.IGetProviderOrderStatsQueryHandler,
            VTOS.Application.Features.Providers.Queries.GetProviderOrderStatsQueryHandler>();
        services.AddScoped<VTOS.Application.Features.Providers.Queries.IGetProviderCatalogQueryHandler,
            VTOS.Application.Features.Providers.Queries.GetProviderCatalogQueryHandler>();
        services.AddScoped<VTOS.Application.Features.Providers.Commands.IUpsertProviderCatalogItemCommandHandler,
            VTOS.Application.Features.Providers.Commands.UpsertProviderCatalogItemCommandHandler>();
        services.AddScoped<VTOS.Application.Features.Providers.Commands.ProviderCatalogVariantCommandHandler>();
        services.AddScoped<VTOS.Application.Features.Providers.Commands.IGetProviderCatalogVariantsQueryHandler>(
            provider => provider.GetRequiredService<VTOS.Application.Features.Providers.Commands.ProviderCatalogVariantCommandHandler>());
        services.AddScoped<VTOS.Application.Features.Providers.Commands.ICreateProviderCatalogVariantCommandHandler>(
            provider => provider.GetRequiredService<VTOS.Application.Features.Providers.Commands.ProviderCatalogVariantCommandHandler>());
        services.AddScoped<VTOS.Application.Features.Providers.Commands.IUpdateProviderCatalogVariantCommandHandler>(
            provider => provider.GetRequiredService<VTOS.Application.Features.Providers.Commands.ProviderCatalogVariantCommandHandler>());
        services.AddScoped<VTOS.Application.Features.Providers.Commands.IDeleteProviderCatalogVariantCommandHandler>(
            provider => provider.GetRequiredService<VTOS.Application.Features.Providers.Commands.ProviderCatalogVariantCommandHandler>());
        services.AddScoped<VTOS.Application.Features.Providers.Commands.IAcceptDirectOrderCommandHandler,
            VTOS.Application.Features.Providers.Commands.AcceptDirectOrderCommandHandler>();
        services.AddScoped<VTOS.Application.Features.Providers.Commands.IUpdateDirectOrderInProductionCommandHandler,
            VTOS.Application.Features.Providers.Commands.UpdateDirectOrderInProductionCommandHandler>();
        services.AddScoped<VTOS.Application.Features.Providers.Commands.IMarkDirectOrderReadyToShipCommandHandler,
            VTOS.Application.Features.Providers.Commands.MarkDirectOrderReadyToShipCommandHandler>();
        services.AddScoped<VTOS.Application.Features.Providers.Commands.IShipDirectOrderCommandHandler,
            VTOS.Application.Features.Providers.Commands.ShipDirectOrderCommandHandler>();

        // Phase 5 - Complaints
        services.AddScoped<ICreateSupportTicketCommandHandler, CreateSupportTicketCommandHandler>();
        services.AddScoped<IGetMySupportTicketsQueryHandler, GetMySupportTicketsQueryHandler>();
        services.AddScoped<IGetMySupportTicketDetailQueryHandler, GetMySupportTicketDetailQueryHandler>();
        services.AddScoped<VTOS.Application.Features.Schools.Queries.IGetSupportTicketDetailQueryHandler,
            VTOS.Application.Features.Schools.Queries.GetSupportTicketDetailQueryHandler>();
        services.AddScoped<VTOS.Application.Features.Schools.Commands.ICloseSupportTicketCommandHandler,
            VTOS.Application.Features.Schools.Commands.CloseSupportTicketCommandHandler>();
        services.AddScoped<VTOS.Application.Features.Providers.Queries.IGetProviderSupportTicketsQueryHandler,
            VTOS.Application.Features.Providers.Queries.GetProviderSupportTicketsQueryHandler>();
        services.AddScoped<VTOS.Application.Features.Providers.Queries.IGetProviderSupportTicketDetailQueryHandler,
            VTOS.Application.Features.Providers.Queries.GetProviderSupportTicketDetailQueryHandler>();
        services.AddScoped<VTOS.Application.Features.Providers.Commands.IRespondSupportTicketCommandHandler,
            VTOS.Application.Features.Providers.Commands.RespondSupportTicketCommandHandler>();

        // Phase 5 - Generic Chat
        services.AddScoped<VTOS.Application.Features.Chat.Queries.IGetChatMessagesQueryHandler,
            VTOS.Application.Features.Chat.Queries.GetChatMessagesQueryHandler>();
        services.AddScoped<VTOS.Application.Features.Chat.Commands.ISendChatMessageCommandHandler,
            VTOS.Application.Features.Chat.Commands.SendChatMessageCommandHandler>();
        services.AddScoped<VTOS.Application.Features.Chat.Commands.ISendUniformProposalCommandHandler,
            VTOS.Application.Features.Chat.Commands.SendUniformProposalCommandHandler>();
        services.AddScoped<VTOS.Application.Features.Chat.Commands.IAcceptUniformProposalCommandHandler,
            VTOS.Application.Features.Chat.Commands.AcceptUniformProposalCommandHandler>();
        services.AddScoped<VTOS.Application.Abstractions.IChatBroadcaster,
            VTOS.Infrastructure.Hubs.SignalRChatBroadcaster>();

        // Contract Module (Phase 2)
        services.AddScoped<VTOS.Application.Features.Contracts.Commands.ICreateContractCommandHandler,
            VTOS.Application.Features.Contracts.Commands.CreateContractCommandHandler>();
        services.AddScoped<VTOS.Application.Features.Contracts.Commands.IUpdateContractPricingCommandHandler,
            VTOS.Application.Features.Contracts.Commands.UpdateContractPricingCommandHandler>();
        services.AddScoped<VTOS.Application.Features.Contracts.Commands.IApproveContractCommandHandler,
            VTOS.Application.Features.Contracts.Commands.ApproveContractCommandHandler>();
        services.AddScoped<VTOS.Application.Features.Contracts.Commands.IRejectContractCommandHandler,
            VTOS.Application.Features.Contracts.Commands.RejectContractCommandHandler>();
        services.AddScoped<VTOS.Application.Features.Contracts.Commands.ICancelContractCommandHandler,
            VTOS.Application.Features.Contracts.Commands.CancelContractCommandHandler>();
        // Contract Signing Flow (OTP + Digital Signature)
        services.AddScoped<VTOS.Application.Features.Contracts.Commands.IRequestSignOTPCommandHandler,
            VTOS.Application.Features.Contracts.Commands.RequestSignOTPCommandHandler>();
        services.AddScoped<VTOS.Application.Features.Contracts.Commands.ISignContractBySchoolCommandHandler,
            VTOS.Application.Features.Contracts.Commands.SignContractBySchoolCommandHandler>();
        services.AddScoped<VTOS.Application.Features.Contracts.Commands.ISignContractByProviderCommandHandler,
            VTOS.Application.Features.Contracts.Commands.SignContractByProviderCommandHandler>();
        services.AddScoped<VTOS.Application.Features.Contracts.Queries.IGetContractsQueryHandler,
            VTOS.Application.Features.Contracts.Queries.GetContractsQueryHandler>();
        services.AddScoped<VTOS.Application.Features.Contracts.Queries.IGetContractDetailQueryHandler,
            VTOS.Application.Features.Contracts.Queries.GetContractDetailQueryHandler>();

        // Contract-based Provider Resolution (for Campaign creation)
        services.AddScoped<VTOS.Application.Features.Schools.Queries.IGetContractedProvidersForOutfitsQueryHandler,
            VTOS.Application.Features.Schools.Queries.GetContractedProvidersForOutfitsQueryHandler>();

        // Admin Module - User Management (UC 3.2.8, 3.2.11)
        services.AddScoped<IGetUserDetailQueryHandler, GetUserDetailQueryHandler>();
        services.AddScoped<IGetUserReportQueryHandler, GetUserReportQueryHandler>();

        // Admin Module - School/Provider Approval (UC 3.2.12, 3.2.13)
        services.AddScoped<IApproveSchoolRequestCommandHandler, ApproveSchoolRequestCommandHandler>();
        services.AddScoped<IApproveProviderRequestCommandHandler, ApproveProviderRequestCommandHandler>();

        // Admin Module - Parent Management (UC 3.5.1, 3.5.2)
        services.AddScoped<IGetParentListQueryHandler, GetParentListQueryHandler>();
        services.AddScoped<IGetParentDetailQueryHandler, GetParentDetailQueryHandler>();

        // Admin Module - Analytics & Dashboard (UC 3.13.1-5)
        services.AddScoped<IGetDashboardAnalyticsQueryHandler, GetDashboardAnalyticsQueryHandler>();
        services.AddScoped<IGetTotalOrdersQueryHandler, GetTotalOrdersQueryHandler>();
        services.AddScoped<IGetTotalQuantityPerItemQueryHandler, GetTotalQuantityPerItemQueryHandler>();
        services.AddScoped<IGetTotalRevenueQueryHandler, GetTotalRevenueQueryHandler>();
        services.AddScoped<IGetPaymentCompletionRateQueryHandler, GetPaymentCompletionRateQueryHandler>();
        services.AddScoped<IGetAdminSemesterPublicationsQueryHandler, GetAdminSemesterPublicationsQueryHandler>();
        services.AddScoped<IGetSemesterMonitorReportQueryHandler, GetSemesterMonitorReportQueryHandler>();

        // Admin Module - Reports & Export (UC 3.13.8-11)
        services.AddScoped<IViewReportQueryHandler, ViewReportQueryHandler>();
        services.AddScoped<IExportReportCommandHandler, ExportReportCommandHandler>();
        services.AddScoped<IGenerateSystemReportCommandHandler, GenerateSystemReportCommandHandler>();
        services.AddScoped<IExportSchoolActivityLogsCommandHandler, ExportSchoolActivityLogsCommandHandler>();

        // Admin Module - Uniform Categories (UC 3.14.1-4)
        services.AddScoped<Application.Features.Admin.Queries.GetCategoriesQueryHandler>();
        services.AddScoped<AddCategoryCommandHandler>();
        services.AddScoped<UpdateCategoryCommandHandler>();
        services.AddScoped<DeleteCategoryCommandHandler>();

        // Admin Module - Settings Configuration (UC 3.14.5-8)
        services.AddScoped<ConfigureSizeTemplateCommandHandler>();
        services.AddScoped<ConfigureDefaultSizeChartCommandHandler>();
        services.AddScoped<ConfigurePaymentMethodCommandHandler>();
        services.AddScoped<ConfigureAITryOnSettingsCommandHandler>();

        // Admin Module - Payment Monitoring (UC 3.15.1)
        services.AddScoped<MonitorPaymentTransactionsQueryHandler>();

        // Phase 6 - Internal Payment
        services.AddScoped<VTOS.Application.Features.Payments.Commands.IPayOrderCommandHandler,
            VTOS.Application.Features.Payments.Commands.PayOrderCommandHandler>();
        services.AddScoped<VTOS.Application.Features.Payments.Commands.IUpdateWalletBankInfoCommandHandler,
            VTOS.Application.Features.Payments.Commands.UpdateWalletBankInfoCommandHandler>();
        services.AddScoped<VTOS.Application.Features.Payments.Commands.IGenerateInvoiceCommandHandler,
            VTOS.Application.Features.Payments.Commands.GenerateInvoiceCommandHandler>();
        services.AddScoped<VTOS.Application.Features.Payments.Queries.IGetParentPaymentHistoryQueryHandler,
            VTOS.Application.Features.Payments.Queries.GetParentPaymentHistoryQueryHandler>();
        services.AddScoped<VTOS.Application.Features.Payments.Queries.IGetProviderRevenueQueryHandler,
            VTOS.Application.Features.Payments.Queries.GetProviderRevenueQueryHandler>();
        services.AddScoped<VTOS.Application.Features.Payments.Queries.IGetProviderPaymentHistoryQueryHandler,
            VTOS.Application.Features.Payments.Queries.GetProviderPaymentHistoryQueryHandler>();
        services.AddScoped<VTOS.Application.Features.Payments.Queries.IGetProviderWalletQueryHandler,
            VTOS.Application.Features.Payments.Queries.GetProviderWalletQueryHandler>();
        services.AddScoped<VTOS.Application.Features.Payments.Queries.IGetProviderWalletTransactionsQueryHandler,
            VTOS.Application.Features.Payments.Queries.GetProviderWalletTransactionsQueryHandler>();
        services.AddScoped<VTOS.Application.Features.Payments.Queries.IGetParentWalletQueryHandler,
            VTOS.Application.Features.Payments.Queries.GetParentWalletQueryHandler>();
        services.AddScoped<VTOS.Application.Features.Payments.Queries.IGetParentWalletTransactionsQueryHandler,
            VTOS.Application.Features.Payments.Queries.GetParentWalletTransactionsQueryHandler>();
        services.AddScoped<VTOS.Application.Features.Users.Commands.ICreateParentWithdrawalRequestCommandHandler,
            VTOS.Application.Features.Users.Commands.CreateParentWithdrawalRequestCommandHandler>();

        // Account Requests Module (System Improvements - Phase 01)
        services.AddScoped<VTOS.Application.Features.AccountRequests.Commands.ISubmitAccountRequestCommandHandler,
            VTOS.Application.Features.AccountRequests.Commands.SubmitAccountRequestCommandHandler>();
        services.AddScoped<VTOS.Application.Features.AccountRequests.Commands.ICreateAccountForRequestCommandHandler,
            VTOS.Application.Features.AccountRequests.Commands.CreateAccountForRequestCommandHandler>();
        services.AddScoped<VTOS.Application.Features.AccountRequests.Commands.IRejectAccountRequestCommandHandler,
            VTOS.Application.Features.AccountRequests.Commands.RejectAccountRequestCommandHandler>();
        services.AddScoped<VTOS.Application.Features.AccountRequests.Queries.IGetAccountRequestsQueryHandler,
            VTOS.Application.Features.AccountRequests.Queries.GetAccountRequestsQueryHandler>();
        services.AddScoped<VTOS.Application.Features.AccountRequests.Queries.IGetAccountRequestDetailQueryHandler,
            VTOS.Application.Features.AccountRequests.Queries.GetAccountRequestDetailQueryHandler>();

        // In-App Notification Service
        services.AddScoped<VTOS.Application.Features.Notifications.INotificationService,
            VTOS.Application.Features.Notifications.NotificationService>();

        // Feedback Module (Campaign Outfit Feedback)
        services.AddScoped<VTOS.Application.Features.Feedbacks.Commands.ISubmitFeedbackCommandHandler,
            VTOS.Application.Features.Feedbacks.Commands.SubmitFeedbackCommandHandler>();
        services.AddScoped<VTOS.Application.Features.Feedbacks.Queries.IGetParentFeedbacksQueryHandler,
            VTOS.Application.Features.Feedbacks.Queries.GetParentFeedbacksQueryHandler>();

        return services;
    }
}


