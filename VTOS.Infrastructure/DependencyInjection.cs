using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VTOS.Application.Abstractions;
using VTOS.Application.Common.Settings;
using VTOS.Application.Features.Admin.Queries;
using VTOS.Application.Features.Admin.Commands;
using VTOS.Application.Features.Auth.Commands;
using VTOS.Application.Features.Auth.Queries;
using VTOS.Application.Features.Public.Queries;
using VTOS.Application.Features.TryOn.Commands.GuestTryOn;
using VTOS.Application.Features.Users.Commands;
using VTOS.Application.Features.Users.Queries;
using VTOS.Infrastructure.ExternalServices.ImageStorage;
using VTOS.Infrastructure.ExternalServices.TryOn;
using VTOS.Infrastructure.Persistence;
using VTOS.Infrastructure.Services;
using AutoMapper;
using VTOS.Application.Features.Children.Commands;
using VTOS.Application.Features.Children.Mappings;
using VTOS.Application.Features.Children.Queries;
using VTOS.Infrastructure.ExternalServices.PayOS;
using VTOS.Application.Features.Orders.Commands;
using VTOS.Application.Features.Orders.Queries;
namespace VTOS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Add In-Memory Cache (reduces DB load for public endpoints)
        services.AddMemoryCache();

        // Add DbContext
        services.AddDbContext<VTOSDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(VTOSDbContext).Assembly.FullName)));

        // Register DbContext as IApplicationDbContext
        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<VTOSDbContext>());

        // Register JWT Settings
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        // Register Email Settings
        services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));

        // Register TryOn Settings (UC-60)
        services.Configure<VirtualTryOnSettings>(configuration.GetSection(VirtualTryOnSettings.SectionName));
        services.Configure<TryOnSettings>(configuration.GetSection(TryOnSettings.SectionName));
        services.Configure<ImgBBSettings>(configuration.GetSection(ImgBBSettings.SectionName));


        // Register PayOS Settings
        services.Configure<PayOSSettings>(configuration.GetSection(PayOSSettings.SectionName));

        // Register Payment Settings
        services.Configure<PaymentSettings>(configuration.GetSection(PaymentSettings.SectionName));

        // Register Frontend Settings
        services.Configure<FrontendSettings>(configuration.GetSection(FrontendSettings.SectionName));

        // Register Services
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // Register TryOn Services with HttpClient (UC-60)
        services.AddHttpClient<IVirtualTryOnService, VirtualTryOnService>();
        services.AddHttpClient<IImageUploadService, ImgBBImageService>();

        //Register PayOS Service
        services.AddHttpClient<IPayOSService, PayOSService>();

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

        // Register Validators
        services.AddValidatorsFromAssemblyContaining<RegisterCommandHandler>();

        // Register Mappers
        services.AddAutoMapper(typeof(ChildProfileMappingProfile).Assembly);

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

        // Parent - Children Management
        services.AddScoped<GetMyChildrenQueryHandler>();
        services.AddScoped<FindChildrenCommandHandler>();


        //Child Infor
        services.AddScoped<IGetMyChildProfileQueryHandler, GetMyChildProfileQueryHandler>();
        services.AddScoped<IGetChildProfileQueryHandler, GetChildProfileQueryHandler>();
        services.AddScoped<IUpdateChildProfileCommandHandler, UpdateChildProfileCommandHandler>();

        // Public Module Handlers (UC 3.3.2, 3.3.3, 3.3.4, 3.3.5)
        services.AddScoped<GetSchoolsQueryHandler>();
        services.AddScoped<Application.Features.Admin.Queries.GetCategoriesQueryHandler>();
        services.AddScoped<GetOutfitDetailQueryHandler>();
        services.AddScoped<GetSchoolDetailQueryHandler>();
        services.AddScoped<GetUniformListQueryHandler>();
        services.AddScoped<GetPublicCampaignDetailQueryHandler>();

        // TryOn Module Handlers (UC-60)
        services.AddScoped<IGuestTryOnCommandHandler, GuestTryOnCommandHandler>();

        // Orders Module Handlers (Checkout, Cancel, Track Status, History)
        services.AddScoped<ICheckoutCommandHandler, CheckoutCommandHandler>();
        services.AddScoped<ICancelOrderCommandHandler, CancelOrderCommandHandler>();
        services.AddScoped<IPaymentWebhookHandler, PaymentWebhookHandler>();
        services.AddScoped<IGetOrderStatusQueryHandler, GetOrderStatusQueryHandler>();
        services.AddScoped<IGetOrderHistoryQueryHandler, GetOrderHistoryQueryHandler>();

        // Background Jobs
        services.AddHostedService<BackgroundJobs.StaleOrderCleanupService>();

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
        services.AddScoped<VTOS.Application.Features.Schools.Queries.IGetCampaignProgressQueryHandler,
            VTOS.Application.Features.Schools.Queries.GetCampaignProgressQueryHandler>();
        services.AddScoped<VTOS.Application.Features.Schools.Queries.IGetSalesReportQueryHandler,
            VTOS.Application.Features.Schools.Queries.GetSalesReportQueryHandler>();
        services.AddScoped<VTOS.Application.Features.Schools.Queries.IGetFeedbackReportQueryHandler,
            VTOS.Application.Features.Schools.Queries.GetFeedbackReportQueryHandler>();

        // School Module - UC-43: Import Student Data
        services.AddScoped<VTOS.Application.Features.Schools.Commands.IImportStudentDataCommandHandler,
            VTOS.Application.Features.Schools.Commands.ImportStudentDataCommandHandler>();

        // School Module - Get School Students
        services.AddScoped<VTOS.Application.Features.Schools.Queries.IGetSchoolStudentsQueryHandler,
            VTOS.Application.Features.Schools.Queries.GetSchoolStudentsQueryHandler>();

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

        services.AddScoped<VTOS.Application.Features.Schools.Commands.IPublishCampaignCommandHandler,
            VTOS.Application.Features.Schools.Commands.PublishCampaignCommandHandler>();

        // School Module - UC 3.9.x: Pre-Order & Production Management
        services.AddScoped<VTOS.Application.Features.Schools.Queries.IGetCampaignListQueryHandler,
            VTOS.Application.Features.Schools.Queries.GetCampaignListQueryHandler>();
        services.AddScoped<VTOS.Application.Features.Schools.Queries.IGetCampaignDetailQueryHandler,
            VTOS.Application.Features.Schools.Queries.GetCampaignDetailQueryHandler>();
        services.AddScoped<VTOS.Application.Features.Schools.Queries.IGetCampaignOrderedItemsQueryHandler,
            VTOS.Application.Features.Schools.Queries.GetCampaignOrderedItemsQueryHandler>();
        services.AddScoped<VTOS.Application.Features.Schools.Queries.IGetCampaignSelectedSizesQueryHandler,
            VTOS.Application.Features.Schools.Queries.GetCampaignSelectedSizesQueryHandler>();
        services.AddScoped<VTOS.Application.Features.Schools.Commands.ILockCampaignCommandHandler,
            VTOS.Application.Features.Schools.Commands.LockCampaignCommandHandler>();
        services.AddScoped<VTOS.Application.Features.Schools.Queries.IGetCampaignSummaryQueryHandler,
            VTOS.Application.Features.Schools.Queries.GetCampaignSummaryQueryHandler>();
        services.AddScoped<VTOS.Application.Features.Schools.Queries.IGetCampaignTotalQuantityQueryHandler,
            VTOS.Application.Features.Schools.Queries.GetCampaignTotalQuantityQueryHandler>();
        services.AddScoped<VTOS.Application.Features.Schools.Commands.IGenerateProductionOrderCommandHandler,
            VTOS.Application.Features.Schools.Commands.GenerateProductionOrderCommandHandler>();
        services.AddScoped<VTOS.Application.Features.Schools.Commands.ISendProductionRequestCommandHandler,
            VTOS.Application.Features.Schools.Commands.SendProductionRequestCommandHandler>();
        services.AddScoped<VTOS.Application.Features.Schools.Commands.IConfirmProductionOrderCommandHandler,
            VTOS.Application.Features.Schools.Commands.ConfirmProductionOrderCommandHandler>();
        services.AddScoped<VTOS.Application.Features.Schools.Queries.IGetProductionComplaintsQueryHandler,
            VTOS.Application.Features.Schools.Queries.GetProductionComplaintsQueryHandler>();
        services.AddScoped<VTOS.Application.Features.Schools.Queries.IGetProductionOrderListQueryHandler,
            VTOS.Application.Features.Schools.Queries.GetProductionOrderListQueryHandler>();
        services.AddScoped<VTOS.Application.Features.Schools.Queries.IGetProductionOrderDetailQueryHandler,
            VTOS.Application.Features.Schools.Queries.GetProductionOrderDetailQueryHandler>();
        services.AddScoped<VTOS.Application.Features.Schools.Queries.IGetProductionOrderItemsQueryHandler,
            VTOS.Application.Features.Schools.Queries.GetProductionOrderItemsQueryHandler>();
        services.AddScoped<VTOS.Application.Features.Schools.Queries.IGetProductionOrderQuantityQueryHandler,
            VTOS.Application.Features.Schools.Queries.GetProductionOrderQuantityQueryHandler>();
        services.AddScoped<VTOS.Application.Features.Schools.Queries.IGetDeliveryDeadlineQueryHandler,
            VTOS.Application.Features.Schools.Queries.GetDeliveryDeadlineQueryHandler>();
        services.AddScoped<VTOS.Application.Features.Schools.Commands.IProcessProductionOrderCommandHandler,
            VTOS.Application.Features.Schools.Commands.ProcessProductionOrderCommandHandler>();
        services.AddScoped<VTOS.Application.Features.Schools.Commands.IRejectProductionOrderCommandHandler,
            VTOS.Application.Features.Schools.Commands.RejectProductionOrderCommandHandler>();

        // School Module - Provider Listing
        services.AddScoped<VTOS.Application.Features.Schools.Queries.IGetProvidersQueryHandler,
            VTOS.Application.Features.Schools.Queries.GetProvidersQueryHandler>();

        // School Module - Withdrawal Request
        services.AddScoped<VTOS.Application.Features.Schools.Commands.ICreateWithdrawalRequestCommandHandler,
            VTOS.Application.Features.Schools.Commands.CreateWithdrawalRequestCommandHandler>();

        // School Module - Bank Account
        services.AddScoped<VTOS.Application.Features.Schools.Commands.IUpdateSchoolBankAccountCommandHandler,
            VTOS.Application.Features.Schools.Commands.UpdateSchoolBankAccountCommandHandler>();

        // Admin Module - Approve Withdrawal
        services.AddScoped<VTOS.Application.Features.Admin.Commands.IApproveWithdrawalCommandHandler,
            VTOS.Application.Features.Admin.Commands.ApproveWithdrawalCommandHandler>();

        // Parent Module - Bank Account
        services.AddScoped<VTOS.Application.Features.Users.Commands.IAddParentBankAccountCommandHandler,
            VTOS.Application.Features.Users.Commands.AddParentBankAccountCommandHandler>();

        // School Module - Get Refund Requests
        services.AddScoped<VTOS.Application.Features.Schools.Queries.IGetSchoolRefundsQueryHandler,
            VTOS.Application.Features.Schools.Queries.GetSchoolRefundsQueryHandler>();

        // Admin Module - Get Withdrawal Requests
        services.AddScoped<VTOS.Application.Features.Admin.Queries.IGetWithdrawalRequestsQueryHandler,
            VTOS.Application.Features.Admin.Queries.GetWithdrawalRequestsQueryHandler>();

        // Provider Module - Profile
        services.AddScoped<VTOS.Application.Features.Providers.Queries.IGetProviderProfileQueryHandler,
            VTOS.Application.Features.Providers.Queries.GetProviderProfileQueryHandler>();
        services.AddScoped<VTOS.Application.Features.Providers.Commands.IUpdateProviderProfileCommandHandler,
            VTOS.Application.Features.Providers.Commands.UpdateProviderProfileCommandHandler>();

        // Provider Module - Production Orders (Phase 3)
        services.AddScoped<VTOS.Application.Features.Providers.Queries.IGetProviderProductionOrderListQueryHandler,
            VTOS.Application.Features.Providers.Queries.GetProviderProductionOrderListQueryHandler>();
        services.AddScoped<VTOS.Application.Features.Providers.Queries.IGetProviderProductionOrderDetailQueryHandler,
            VTOS.Application.Features.Providers.Queries.GetProviderProductionOrderDetailQueryHandler>();
        services.AddScoped<VTOS.Application.Features.Providers.Commands.IAcceptProductionOrderCommandHandler,
            VTOS.Application.Features.Providers.Commands.AcceptProductionOrderCommandHandler>();
        services.AddScoped<VTOS.Application.Features.Providers.Commands.ICompleteProductionOrderCommandHandler,
            VTOS.Application.Features.Providers.Commands.CompleteProductionOrderCommandHandler>();
        services.AddScoped<VTOS.Application.Features.Providers.Commands.IProviderRejectProductionOrderCommandHandler,
            VTOS.Application.Features.Providers.Commands.ProviderRejectProductionOrderCommandHandler>();

        // Provider Module - Delivery (Phase 4)
        services.AddScoped<VTOS.Application.Features.Providers.Commands.IDeliverProductionOrderCommandHandler,
            VTOS.Application.Features.Providers.Commands.DeliverProductionOrderCommandHandler>();
        services.AddScoped<VTOS.Application.Features.Providers.Queries.IGetDeliveryStatusQueryHandler,
            VTOS.Application.Features.Providers.Queries.GetDeliveryStatusQueryHandler>();

        // School Module - Delivery & Distribution (Phase 4)
        services.AddScoped<VTOS.Application.Features.Schools.Commands.IConfirmDeliveryCommandHandler,
            VTOS.Application.Features.Schools.Commands.ConfirmDeliveryCommandHandler>();
        services.AddScoped<VTOS.Application.Features.Schools.Queries.IGetVerifyQuantityQueryHandler,
            VTOS.Application.Features.Schools.Queries.GetVerifyQuantityQueryHandler>();
        services.AddScoped<VTOS.Application.Features.Schools.Commands.IReportDefectCommandHandler,
            VTOS.Application.Features.Schools.Commands.ReportDefectCommandHandler>();
        services.AddScoped<VTOS.Application.Features.Schools.Commands.IDistributeOrdersCommandHandler,
            VTOS.Application.Features.Schools.Commands.DistributeOrdersCommandHandler>();
        services.AddScoped<VTOS.Application.Features.Schools.Queries.IGetDistributionStatusQueryHandler,
            VTOS.Application.Features.Schools.Queries.GetDistributionStatusQueryHandler>();
        services.AddScoped<VTOS.Application.Features.Schools.Queries.IGetSchoolDeliveryStatusQueryHandler,
            VTOS.Application.Features.Schools.Queries.GetSchoolDeliveryStatusQueryHandler>();

        // Phase 5 - Complaints
        services.AddScoped<VTOS.Application.Features.Schools.Queries.IGetComplaintDetailQueryHandler,
            VTOS.Application.Features.Schools.Queries.GetComplaintDetailQueryHandler>();
        services.AddScoped<VTOS.Application.Features.Schools.Commands.ICloseComplaintCommandHandler,
            VTOS.Application.Features.Schools.Commands.CloseComplaintCommandHandler>();
        services.AddScoped<VTOS.Application.Features.Providers.Queries.IGetProviderComplaintsQueryHandler,
            VTOS.Application.Features.Providers.Queries.GetProviderComplaintsQueryHandler>();
        services.AddScoped<VTOS.Application.Features.Providers.Queries.IGetProviderComplaintDetailQueryHandler,
            VTOS.Application.Features.Providers.Queries.GetProviderComplaintDetailQueryHandler>();
        services.AddScoped<VTOS.Application.Features.Providers.Commands.IRespondComplaintCommandHandler,
            VTOS.Application.Features.Providers.Commands.RespondComplaintCommandHandler>();

        // Phase 5 - Generic Chat
        services.AddScoped<VTOS.Application.Features.Chat.Queries.IGetChatMessagesQueryHandler,
            VTOS.Application.Features.Chat.Queries.GetChatMessagesQueryHandler>();
        services.AddScoped<VTOS.Application.Features.Chat.Commands.ISendChatMessageCommandHandler,
            VTOS.Application.Features.Chat.Commands.SendChatMessageCommandHandler>();
        services.AddScoped<VTOS.Application.Abstractions.IChatBroadcaster,
            VTOS.Infrastructure.Hubs.SignalRChatBroadcaster>();

        // Contract Module (Phase 2)
        services.AddScoped<VTOS.Application.Features.Contracts.Commands.ICreateContractCommandHandler,
            VTOS.Application.Features.Contracts.Commands.CreateContractCommandHandler>();
        services.AddScoped<VTOS.Application.Features.Contracts.Commands.IApproveContractCommandHandler,
            VTOS.Application.Features.Contracts.Commands.ApproveContractCommandHandler>();
        services.AddScoped<VTOS.Application.Features.Contracts.Commands.IRejectContractCommandHandler,
            VTOS.Application.Features.Contracts.Commands.RejectContractCommandHandler>();
        services.AddScoped<VTOS.Application.Features.Contracts.Queries.IGetContractsQueryHandler,
            VTOS.Application.Features.Contracts.Queries.GetContractsQueryHandler>();
        services.AddScoped<VTOS.Application.Features.Contracts.Queries.IGetContractDetailQueryHandler,
            VTOS.Application.Features.Contracts.Queries.GetContractDetailQueryHandler>();

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
        services.AddScoped<VTOS.Application.Features.Payments.Commands.IPayProviderCommandHandler,
            VTOS.Application.Features.Payments.Commands.PayProviderCommandHandler>();
        services.AddScoped<VTOS.Application.Features.Payments.Commands.IRefundOrderCommandHandler,
            VTOS.Application.Features.Payments.Commands.RefundOrderCommandHandler>();
        services.AddScoped<VTOS.Application.Features.Payments.Commands.IUpdateWalletBankInfoCommandHandler,
            VTOS.Application.Features.Payments.Commands.UpdateWalletBankInfoCommandHandler>();
        services.AddScoped<VTOS.Application.Features.Payments.Commands.IGenerateInvoiceCommandHandler,
            VTOS.Application.Features.Payments.Commands.GenerateInvoiceCommandHandler>();
        services.AddScoped<VTOS.Application.Features.Payments.Queries.IGetSchoolWalletQueryHandler,
            VTOS.Application.Features.Payments.Queries.GetSchoolWalletQueryHandler>();
        services.AddScoped<VTOS.Application.Features.Payments.Queries.IGetWalletTransactionsQueryHandler,
            VTOS.Application.Features.Payments.Queries.GetWalletTransactionsQueryHandler>();
        services.AddScoped<VTOS.Application.Features.Payments.Queries.IGetParentPaymentHistoryQueryHandler,
            VTOS.Application.Features.Payments.Queries.GetParentPaymentHistoryQueryHandler>();
        services.AddScoped<VTOS.Application.Features.Payments.Queries.IGetProviderRevenueQueryHandler,
            VTOS.Application.Features.Payments.Queries.GetProviderRevenueQueryHandler>();
        services.AddScoped<VTOS.Application.Features.Payments.Queries.IGetProviderPaymentHistoryQueryHandler,
            VTOS.Application.Features.Payments.Queries.GetProviderPaymentHistoryQueryHandler>();

        return services;
    }
}


