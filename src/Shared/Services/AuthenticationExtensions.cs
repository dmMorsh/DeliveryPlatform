using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace Shared.Services;

public static class AuthenticationExtensions
{
    public static void AddExtededAuthentication(this WebApplicationBuilder builder)
    {
        var jwtKey = ConfigurationGuard.GetRequired(builder.Configuration, builder.Environment, "Jwt:Key", "dev-key");
        var jwtIssuer = ConfigurationGuard.GetRequired(builder.Configuration, builder.Environment, "Jwt:Issuer", "identity-service");
        var jwtAudience = ConfigurationGuard.GetRequired(builder.Configuration, builder.Environment, "Jwt:Audience", "platform-api");

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,

                    ValidIssuer = jwtIssuer,
                    ValidAudience = jwtAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Headers["authorization"];
                        if (!string.IsNullOrEmpty(accessToken))
                        {
                            context.Token = accessToken.ToString().Replace("Bearer ", "");
                        }
                        return Task.CompletedTask;
                    }
                };
            });
    }

    public static void AddExtededCors(this WebApplicationBuilder builder)
    {
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                if (builder.Environment.IsDevelopment())
                {
                    policy.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader();
                    return;
                }

                var paymentCorsOrigins = ConfigurationGuard.GetRequiredArray(builder.Configuration, builder.Environment, "Cors:AllowedOrigins");
                policy.WithOrigins(paymentCorsOrigins)
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            });
        });
    }
}