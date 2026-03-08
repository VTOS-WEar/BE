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

        // Public Module Handlers (UC-57, UC-58, UC-59)
        services.AddScoped<GetSchoolsQueryHandler>();
        services.AddScoped<GetCategoriesQueryHandler>();
        services.AddScoped<GetOutfitDetailQueryHandler>();

        // TryOn Module Handlers (UC-60)
        services.AddScoped<IGuestTryOnCommandHandler, GuestTryOnCommandHandler>();

        // Orders Module Handlers (Checkout, Cancel, Track Status, History)
        services.AddScoped<ICheckoutCommandHandler, CheckoutCommandHandler>();
        services.AddScoped<ICancelOrderCommandHandler, CancelOrderCommandHandler>();
        services.AddScoped<IPaymentWebhookHandler, PaymentWebhookHandler>();
        services.AddScoped<IGetOrderStatusQueryHandler, GetOrderStatusQueryHandler>();
        services.AddScoped<IGetOrderHistoryQueryHandler, GetOrderHistoryQueryHandler>();

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

        // School Module - UC-44: Publish Campaign
        services.AddScoped<VTOS.Application.Features.Schools.Commands.IPublishCampaignCommandHandler,
            VTOS.Application.Features.Schools.Commands.PublishCampaignCommandHandler>();

        return services;
    }
}

