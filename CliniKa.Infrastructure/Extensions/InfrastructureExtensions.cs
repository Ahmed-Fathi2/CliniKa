using CliniKa.Application.Abstraction;
using CliniKa.Application.Abstractions.Authentication;
using CliniKa.Application.Abstractions.Services;
using CliniKa.Application.Services;
using CliniKa.Domain.Repositories;
using CliniKa.Infrastructure.Authentication;
using CliniKa.Infrastructure.Options;
using CliniKa.Infrastructure.Persistence.Context;
using CliniKa.Infrastructure.Persistence.Identity;
using CliniKa.Infrastructure.Repositories;
using CliniKa.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace CliniKa.Infrastructure.Extensions
{
    public static class InfrastructureExtensions
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // Bind JwtOptions
            services.AddOptions<JwtOptions>()
                .Bind(configuration.GetSection(JwtOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection")));

            services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                options.SignIn.RequireConfirmedEmail = false;
                options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
            })
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();

            // Authentication & JWT configuration
            var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>();

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions!.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
                    ValidateLifetime = true
                };
            });

            // Configure SendGrid
            services.Configure<SendGridOptions>(configuration.GetSection(SendGridOptions.SectionName));
            services.AddTransient<IEmailSender, SendGridEmailSender>();

            // Configure Activepieces LangFlow Service (uses Node.js helper for SSL compatibility)
            services.AddHttpClient();
            services.AddScoped<ILangFlowService, LangFlowService>();

            // Register repositories
            services.AddScoped(typeof(IGenericRepository<,>), typeof(GenericRepository<,>));
            services.AddScoped<IClinicalSessionRepository, ClinicalSessionRepository>();
            services.AddScoped<IPatientRepository, PatientRepository>();
            services.AddScoped<IDoctorRepository, DoctorRepository>();
            services.AddScoped<IPrescribedMedicationsRepository, PrescribedMedicationsRepository>();
            services.AddScoped<IActiveMedicationsRepository, ActiveMedicationsRepository>();
            services.AddScoped<ISessionAIReportsRepository, SessionAIReportsRepository>();
            services.AddScoped<IChatSessionRepository, ChatSessionRepository>();
            services.AddScoped<IDiseaseRepository, DiseaseRepository>();
            services.AddScoped<IAllergyRepository, AllergyRepository>();

            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IJwtProvider, JwtProvider>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IRoleService, RoleService>();
            services.AddScoped<IFileService, FileService>();
            services.AddScoped<ISessionService, SessionService>();
            services.AddScoped<IChatService, ChatService>();

            // SSE Notification Service — MUST be Singleton so the same instance
            // is shared between the SSE endpoint (reader) and the callback (writer).
            services.AddSingleton<IDiagnosisNotificationService, SseNotificationService>();
            services.AddScoped<IPatientService, PatientService>();

            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUserService, CurrentUserService>();

            return services;
        }
    }
}