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

        // JWT Authentication
        public static void ConfigureJWT(this IServiceCollection services, IConfiguration configuration)
        {
            var jwtSettings = configuration.GetSection("Jwt");
            var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

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
                    RoleClaimType = "role",
                    NameClaimType = "name"
                };
            });
        }

        // CORS Configuration
        public static void ConfigureCORS(this IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll",
                    builder => builder
                        .AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader());
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