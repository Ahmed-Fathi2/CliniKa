using FluentValidation;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using CliniKa.Application.Validators;
using CliniKa.Application.DTOs.Authentication;
using CliniKa.Application.DTOs.Patient;
using CliniKa.Application.DTOs.User;
using CliniKa.Application.DTOs.Role;

using CliniKa.Application.DTOs.Disease;
using CliniKa.Application.DTOs.Allergy;

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
            services.AddScoped<IValidator<CreateUserRequest>, CreateUserRequestValidator>();
            services.AddScoped<IValidator<CreateRoleRequest>, CreateRoleRequestValidator>();
            services.AddScoped<IValidator<AddMedicalInfoRequest>, AddMedicalInfoRequestValidator>();
            services.AddScoped<IValidator<AddMedicationRequest>, AddMedicationRequestValidator>();
            services.AddScoped<IValidator<AddDiseaseRequest>, AddDiseaseRequestValidator>();
            services.AddScoped<IValidator<AddAllergyRequest>, AddAllergyRequestValidator>();
            services.AddScoped<IValidator<CreateDiseaseRequest>, CreateDiseaseRequestValidator>();
            services.AddScoped<IValidator<UpdateDiseaseRequest>, UpdateDiseaseRequestValidator>();
            services.AddScoped<IValidator<CreateAllergyRequest>, CreateAllergyRequestValidator>();
            services.AddScoped<IValidator<UpdateAllergyRequest>, UpdateAllergyRequestValidator>();

            // Mapster configuration
            var config = TypeAdapterConfig.GlobalSettings;
            config.Scan(Assembly.GetExecutingAssembly());
            services.AddSingleton(config);
            services.AddScoped<IMapper, ServiceMapper>();

            return services;
        }
    }
}
