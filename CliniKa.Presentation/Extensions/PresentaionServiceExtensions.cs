using Microsoft.OpenApi;

namespace CliniKa.Presentation.Extensions
{
    public static class PresentationServiceExtensions
    {
        public static IServiceCollection AddPresentation(this IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();

            services.AddSwaggerGen(options =>
            {
                const string schemeId = "Bearer";

                options.AddSecurityDefinition(schemeId, new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer", // lowercase per RFC 7235
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Enter your JWT token. Example: eyJhbGci..."
                });

                // Note: AddSecurityRequirement now takes a delegate, not a static object
                options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
                {
                    { new OpenApiSecuritySchemeReference(schemeId, document), new List<string>() }
                });
            });

            return services;
        }
    }
}