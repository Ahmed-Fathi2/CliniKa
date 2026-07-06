using FluentValidation;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using CliniKa.Application.Validators;
using CliniKa.Application.DTOs.Authentication;

namespace CliniKa.Application.Extensions
{
    public static class ApplicationExtensionServices
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            // Explicitly register validators
            services.AddScoped<IValidator<ConfirmEmailDto>, ConfirmEmailValidator>();
            services.AddScoped<IValidator<LoginRequest>, LoginRequestValidator>();
            services.AddScoped<IValidator<RegisterRequest>, RegisterRequestValidator>();
            services.AddScoped<IValidator<ForgetPasswordRequest>, ForgetPasswordRequestValidator>();
            services.AddScoped<IValidator<ResetPasswordRequest>, ResetPasswordRequestValidator>();

            // Mapster configuration
            var config = TypeAdapterConfig.GlobalSettings;
            config.Scan(Assembly.GetExecutingAssembly());
            services.AddSingleton(config);
            services.AddScoped<IMapper, ServiceMapper>();

            return services;
        }
    }
}
