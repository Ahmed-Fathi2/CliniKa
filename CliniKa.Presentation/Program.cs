using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using CliniKa.Presentation.Extensions;
using CliniKa.Infrastructure.Extensions;
using CliniKa.Application.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// CORS 

builder.Services.AddCors(options =>
{
    // General API policy (REST endpoints)
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
//builder.Services.AddOpenApi();

builder.Services.AddInfrastructure(builder.Configuration)
                .AddApplication()
                .AddPresentation();

var app = builder.Build();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
    app.UseSwagger();
    app.UseSwaggerUI();
    //app.MapOpenApi();
//}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Automatic Database Migration on Startup (Only on Server)
if (!app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<CliniKa.Infrastructure.Persistence.Context.AppDbContext>();
            if (context.Database.GetPendingMigrations().Any())
            {
                context.Database.Migrate();
            }
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred while migrating the database.");
        }
    }
}

app.Run();
