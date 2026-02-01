using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VTOS.Application.Abstractions;
using VTOS.Application.Features.Auth.Commands;
using VTOS.Application.Features.Auth.Queries;
using VTOS.Application.Features.Admin.Queries;
using VTOS.Application.Features.Public.Queries;
using VTOS.Application.Features.TryOn.Commands.GuestTryOn;
using VTOS.Infrastructure.Persistence;
using VTOS.Infrastructure.Services;
using VTOS.Application.Features.Users.Queries;
using VTOS.Application.Features.Users.Commands;
using VTOS.Infrastructure.ExternalServices.TryOn;
using VTOS.Infrastructure.ExternalServices.ImageStorage;

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

        // Register Services
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // Register TryOn Services with HttpClient (UC-60)
        services.AddHttpClient<IVirtualTryOnService, VirtualTryOnService>();
        services.AddHttpClient<IImageUploadService, ImgBBImageService>();

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

        //View User List & Feedbacks
        services.AddScoped<IGetAllUsersQueryHandler, GetAllUsersQueryHandler>();
        services.AddScoped<IGetAllFeedbacksQueryHandler, GetAllFeedbacksQueryHandler>();

        //Parent Personal Infor
        services.AddScoped<IGetProfileQueryHandler, GetProfileQueryHandler>();
        services.AddScoped<IUpdateProfileCommandHandler, UpdateProfileCommandHandler>();
        services.AddScoped<IUpdateAvatarCommandHandler, UpdateAvatarCommandHandler>();
        services.AddScoped<UpdateProfileCommandHandler>();
        services.AddScoped<UpdateAvatarCommandHandler>();

        // Public Module Handlers (UC-57, UC-58, UC-59)
        services.AddScoped<GetSchoolsQueryHandler>();
        services.AddScoped<GetCategoriesQueryHandler>();
        services.AddScoped<GetOutfitDetailQueryHandler>();

        // TryOn Module Handlers (UC-60)
        services.AddScoped<IGuestTryOnCommandHandler, GuestTryOnCommandHandler>();

        return services;
    }
}

