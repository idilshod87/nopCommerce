using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Nop.Core.Configuration;
using Nop.Core.Infrastructure;
using Nop.Services.Authentication.External;
using System.Text;

namespace Nop.Plugin.Misc.WebApi.Frontend.Infrastructure;

/// <summary>
/// Represents registrar of JWT Bearer authentication service
/// </summary>
public class JwtBearerAuthenticationRegistrar : IExternalAuthenticationRegistrar
{
    /// <summary>
    /// Configure JWT Bearer authentication scheme
    /// </summary>
    /// <param name="builder">Authentication builder</param>
    public void Configure(AuthenticationBuilder builder)
    {
        // Read configuration from "Api" section (same as Nop.Plugin.Api uses)
        var configuration = EngineContext.Current.Resolve<IConfiguration>();
        var apiConfigSection = configuration?.GetSection("Api");
        
        if (apiConfigSection != null)
        {
            // Read SecurityKey from configuration (or use default)
            var securityKey = apiConfigSection["SecurityKey"] 
                ?? "NowIsTheTimeForAllGoodMenToComeToTheAideOfTheirCountry"; // default from Nop.Plugin.Api
            
            var allowedClockSkewInMinutes = 5; // default value
            if (int.TryParse(apiConfigSection["AllowedClockSkewInMinutes"], out var clockSkew))
                allowedClockSkewInMinutes = clockSkew;

            if (!string.IsNullOrEmpty(securityKey))
            {
                builder.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, jwtBearerOptions =>
                {
                    jwtBearerOptions.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(securityKey)),
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromMinutes(allowedClockSkewInMinutes)
                    };

                    // Map JWT events to handle authentication
                    jwtBearerOptions.Events = new JwtBearerEvents
                    {
                        OnTokenValidated = async context =>
                        {
                            // Token is valid, but we'll set customer in middleware
                            await Task.CompletedTask;
                        }
                    };
                });

                // Clear claim type map to preserve original claim names
                JsonWebTokenHandler.DefaultInboundClaimTypeMap.Clear();
            }
        }
    }
}

