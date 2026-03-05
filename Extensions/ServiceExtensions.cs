using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using messenger.Data;
using System.IdentityModel.Tokens.Jwt;
using messenger.Models;
using messenger.Repositories.Implementations;
using messenger.Repositories.Interfaces;
using messenger.Services.Implementations;
using messenger.Services.Interfaces;
using System.Security.Claims;
using System.Text;

namespace messenger.Extensions
{
    public static class ServiceExtensions
    {
        // Database Configuration
        public static void ConfigureDatabase(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            var serverVersion = ServerVersion.AutoDetect(connectionString);

            services.AddDbContext<MessengerContext>(options =>
                options.UseMySql(connectionString, serverVersion)
            );
        }

        // SignalR Configuration
        public static void ConfigureSignalR(this IServiceCollection services)
        {
            services.AddSignalR(options =>
            {
                // Enable detailed errors (chỉ dùng trong Development)
                options.EnableDetailedErrors = true;

                // Cấu hình timeout
                options.KeepAliveInterval = TimeSpan.FromSeconds(15);
                options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);

                // Max message size (2MB)
                options.MaximumReceiveMessageSize = 2 * 1024 * 1024;

                // Streaming
                options.StreamBufferCapacity = 10;
            });
        }

        // JWT Authentication
        public static void ConfigureJWT(this IServiceCollection services, IConfiguration configuration)
        {
            var jwtSettings = configuration.GetSection("Jwt");
            var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidAudience = jwtSettings["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ClockSkew = TimeSpan.Zero,
                    RoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role",
                    NameClaimType = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"
                };


                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;

                        // Nếu request đến SignalR hub và có token
                        if (!string.IsNullOrEmpty(accessToken) &&
                            path.StartsWithSegments("/hubs"))
                        {
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    }
                };

            });
        }

        // CORS Configuration
        public static void ConfigureCORS(this IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", builder =>
                {
                    builder
                        .WithOrigins(
                            "http://localhost:3000",     // React
                            "http://localhost:5173"     // Vite 
                        )
                        .SetIsOriginAllowed(_ => true)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();  // ← QUAN TRỌNG cho SignalR!
                });
            });
        }

        // Register all Repositories
        public static void ConfigureRepositories(this IServiceCollection services)
        {
            // Thêm các repository khác ở đây
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        }

        // Register all Services
        public static void ConfigureServices(this IServiceCollection services)
        {
            // Thêm các service khác ở đây
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IJwtService, JwtService>();
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        }

        // Other Configurations
        public static void ConfigureOtherServices(this IServiceCollection services)
        {
            // Password Hasher
            services.AddScoped<IPasswordHasher<Users>, PasswordHasher<Users>>();

            // AutoMapper
            services.AddAutoMapper(typeof(Program));
        }
    }
}