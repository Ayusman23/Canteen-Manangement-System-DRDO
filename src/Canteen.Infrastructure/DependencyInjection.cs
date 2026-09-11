using System.Text;
using Canteen.Application.Common.Interfaces;
using Canteen.Infrastructure.Identity;
using Canteen.Infrastructure.Persistence;
using Canteen.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Canteen.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var rawPostgres = configuration["DATABASE_URL"] 
            ?? configuration["POSTGRES_URL"] 
            ?? configuration.GetConnectionString("PostgreSqlConnection") 
            ?? configuration.GetConnectionString("DefaultConnection");

        var postgresConnection = ParseDatabaseUrl(rawPostgres);
        var sqliteConnection = configuration.GetConnectionString("SqliteConnection") 
            ?? "Data Source=canteen.db";

        services.AddDbContext<CanteenDbContext>((sp, options) =>
        {
            if (!string.IsNullOrWhiteSpace(postgresConnection) && 
                postgresConnection.Contains("Host=", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    options.UseNpgsql(postgresConnection, npgsqlOptions =>
                    {
                        npgsqlOptions.MigrationsAssembly(typeof(CanteenDbContext).Assembly.FullName);
                        npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorCodesToAdd: null);
                    });
                    return;
                }
                catch
                {
                    // Fallback to SQLite if PostgreSQL fails to configure
                }
            }

            // Fallback SQLite
            options.UseSqlite(sqliteConnection, sqliteOptions =>
            {
                sqliteOptions.MigrationsAssembly(typeof(CanteenDbContext).Assembly.FullName);
            });
        });

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<CanteenDbContext>());

        // Identity Configuration
        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 6;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireLowercase = false;

            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;

            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<CanteenDbContext>()
        .AddDefaultTokenProviders();

        // JWT Service
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        // JWT Authentication
        var secretKey = configuration["JWT_SECRET"] 
            ?? configuration["Jwt:Secret"] 
            ?? "DRDO_Canteen_Classified_Ultra_Secure_Key_2026_Enterprise_Production_Standard!";
        var issuer = configuration["Jwt:Issuer"] ?? "DRDO-Canteen-Security";
        var audience = configuration["Jwt:Audience"] ?? "DRDO-Canteen-Clients";

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            // Support SignalR JWT in query string
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;
                    if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/canteen"))
                    {
                        context.Token = accessToken;
                    }
                    return Task.CompletedTask;
                }
            };
        });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("EmployeeOnly", policy => policy.RequireRole("Employee", "CanteenManager"));
            options.AddPolicy("KitchenOperatorOnly", policy => policy.RequireRole("KitchenOperator", "CanteenManager"));
            options.AddPolicy("CanteenManagerOnly", policy => policy.RequireRole("CanteenManager"));
            options.AddPolicy("KitchenOrManager", policy => policy.RequireRole("KitchenOperator", "CanteenManager"));
        });

        return services;
    }

    private static string? ParseDatabaseUrl(string? databaseUrl)
    {
        if (string.IsNullOrWhiteSpace(databaseUrl)) return null;

        if (databaseUrl.Contains("Host=", StringComparison.OrdinalIgnoreCase))
        {
            return databaseUrl;
        }

        try
        {
            if (databaseUrl.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
                databaseUrl.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
            {
                var uri = new Uri(databaseUrl);
                var userInfo = uri.UserInfo.Split(':');
                var user = userInfo.Length > 0 ? Uri.UnescapeDataString(userInfo[0]) : "";
                var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
                var host = uri.Host;
                var port = uri.Port > 0 ? uri.Port : 5432;
                var database = uri.AbsolutePath.TrimStart('/');

                return $"Host={host};Port={port};Database={database};Username={user};Password={password};Ssl Mode=Require;Trust Server Certificate=true;";
            }
        }
        catch
        {
            // Ignore parse errors and fallback
        }

        return databaseUrl;
    }
}
