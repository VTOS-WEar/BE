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

        //Child Infor
        services.AddScoped<IGetMyChildProfileQueryHandler, GetMyChildProfileQueryHandler>();
        services.AddScoped<IGetChildProfileQueryHandler, GetChildProfileQueryHandler>();
        services.AddScoped<IUpdateChildProfileCommandHandler, UpdateChildProfileCommandHandler>();

        // Public Module Handlers (UC 3.3.2, 3.3.3, 3.3.4, 3.3.5)
        services.AddScoped<GetSchoolsQueryHandler>();
        services.AddScoped<GetCategoriesQueryHandler>();
        services.AddScoped<GetOutfitDetailQueryHandler>();
        services.AddScoped<GetSchoolDetailQueryHandler>();
        services.AddScoped<GetUniformListQueryHandler>();

        // TryOn Module Handlers (UC-60)
        services.AddScoped<IGuestTryOnCommandHandler, GuestTryOnCommandHandler>();

        // Orders Module Handlers (Checkout, Cancel, Track Status, History)
        services.AddScoped<ICheckoutCommandHandler, CheckoutCommandHandler>();
        services.AddScoped<ICancelOrderCommandHandler, CancelOrderCommandHandler>();
        services.AddScoped<IPaymentWebhookHandler, PaymentWebhookHandler>();
        services.AddScoped<IGetOrderStatusQueryHandler, GetOrderStatusQueryHandler>();
        services.AddScoped<IGetOrderHistoryQueryHandler, GetOrderHistoryQueryHandler>();

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

        return services;
    }
}


