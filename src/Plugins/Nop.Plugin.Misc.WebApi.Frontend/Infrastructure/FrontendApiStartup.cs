using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Nop.Core.Infrastructure;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Nop.Plugin.Misc.WebApi.Frontend.Infrastructure;

/// <summary>
/// Swagger operation filter to handle file uploads with [FromForm] attribute
/// Based on: https://github.com/domaindrivendev/Swashbuckle.AspNetCore/issues/1297
/// </summary>
public class FileUploadOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var fileParameters = context.ApiDescription.ParameterDescriptions
            .Where(p => p.Source == Microsoft.AspNetCore.Mvc.ModelBinding.BindingSource.Form &&
                       (p.ModelMetadata?.ModelType == typeof(IFormFile) || 
                        p.ModelMetadata?.ModelType == typeof(IFormFileCollection)))
            .ToList();

        if (!fileParameters.Any())
            return;

        // Remove file parameters from parameters list since they'll be in the request body
        if (operation.Parameters != null)
        {
            var paramsToRemove = operation.Parameters
                .Where(p => fileParameters.Any(fp => fp.Name.Equals(p.Name, StringComparison.OrdinalIgnoreCase)))
                .ToList();
            
            foreach (var param in paramsToRemove)
            {
                operation.Parameters.Remove(param);
            }
        }

        // Create or update request body for multipart/form-data
        var content = new Dictionary<string, OpenApiMediaType>
        {
            ["multipart/form-data"] = new OpenApiMediaType
            {
                Schema = new OpenApiSchema
                {
                    Type = "object",
                    Properties = fileParameters.ToDictionary(
                        fp => fp.Name,
                        fp => new OpenApiSchema
                        {
                            Type = "string",
                            Format = "binary",
                            Description = "File to upload"
                        }
                    ),
                    Required = fileParameters.Where(fp => fp.IsRequired).Select(fp => fp.Name).ToHashSet()
                }
            }
        };

        if (operation.RequestBody != null)
        {
            // Merge with existing content
            foreach (var existingContent in operation.RequestBody.Content)
            {
                if (!content.ContainsKey(existingContent.Key))
                {
                    content[existingContent.Key] = existingContent.Value;
                }
            }
        }

        operation.RequestBody = new OpenApiRequestBody
        {
            Required = true,
            Content = content
        };
    }
}

/// <summary>
/// Custom JSON output formatter that applies camelCase only for /public-api routes
/// </summary>
public class PublicApiCamelCaseJsonFormatter : NewtonsoftJsonOutputFormatter
{
    public PublicApiCamelCaseJsonFormatter(JsonSerializerSettings serializerSettings, ArrayPool<char> charPool, MvcOptions mvcOptions)
        : base(serializerSettings, charPool, mvcOptions)
    {
    }

    public override bool CanWriteResult(OutputFormatterCanWriteContext context)
    {
        var path = context.HttpContext.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
        return path.StartsWith("/public-api/") || path.Equals("/public-api");
    }
}

/// <summary>
/// Swagger schema filter that converts property names to camelCase
/// </summary>
public class CamelCaseSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema.Properties == null || schema.Properties.Count == 0)
            return;

        // Convert all property names to camelCase
        var newProperties = new Dictionary<string, OpenApiSchema>();
        foreach (var property in schema.Properties)
        {
            var camelCaseName = ToCamelCase(property.Key);
            newProperties[camelCaseName] = property.Value;
        }
        schema.Properties = newProperties;

        // Also update Required list if it exists
        if (schema.Required != null && schema.Required.Count > 0)
        {
            schema.Required = schema.Required.Select(ToCamelCase).ToHashSet();
        }
    }

    private static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name) || char.IsLower(name[0]))
            return name;

        return char.ToLowerInvariant(name[0]) + name.Substring(1);
    }
}

/// <summary>
/// Swagger operation filter that converts parameter names to camelCase
/// </summary>
public class CamelCaseParameterFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (operation.Parameters == null || operation.Parameters.Count == 0)
            return;

        // Convert all parameter names to camelCase
        foreach (var parameter in operation.Parameters)
        {
            if (!string.IsNullOrEmpty(parameter.Name))
            {
                parameter.Name = ToCamelCase(parameter.Name);
            }
        }

        // Also handle request body schema properties if present
        if (operation.RequestBody?.Content != null)
        {
            foreach (var content in operation.RequestBody.Content.Values)
            {
                if (content.Schema?.Properties != null)
                {
                    var newProperties = new Dictionary<string, OpenApiSchema>();
                    foreach (var property in content.Schema.Properties)
                    {
                        var camelCaseName = ToCamelCase(property.Key);
                        newProperties[camelCaseName] = property.Value;
                    }
                    content.Schema.Properties = newProperties;

                    // Update Required list if it exists
                    if (content.Schema.Required != null && content.Schema.Required.Count > 0)
                    {
                        content.Schema.Required = content.Schema.Required.Select(ToCamelCase).ToHashSet();
                    }
                }
            }
        }
    }

    private static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name) || char.IsLower(name[0]))
            return name;

        return char.ToLowerInvariant(name[0]) + name.Substring(1);
    }
}

/// <summary>
/// API startup for public frontend Web API.
/// Configures endpoint routing for all frontend API controllers and Swagger UI.
/// </summary>
public class FrontendApiStartup : INopStartup
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Add custom camelCase formatter for /public-api routes only
        // Use PostConfigure to add formatter after all other MVC configurations
        services.PostConfigure<MvcOptions>(options =>
        {
            // Insert our custom formatter at the beginning
            // It will only apply to /public-api routes based on CanWriteResult check
            var camelCaseSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                NullValueHandling = NullValueHandling.Ignore,
                DateFormatHandling = DateFormatHandling.IsoDateFormat
            };
            
            options.OutputFormatters.Insert(0, new PublicApiCamelCaseJsonFormatter(
                camelCaseSettings,
                ArrayPool<char>.Shared,
                options));
        });

        // Configure Swagger for frontend API
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("frontend-v1", new OpenApiInfo
            {
                Title = "NopCommerce Frontend API",
                Version = "v1",
                Description = "Public API for mobile applications and frontend clients. Compatible with NopStation Cart API routes."
            });

            // Include XML comments if available
            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }

            // Configure tags and restrict included actions to this plugin only
            options.TagActionsBy(api =>
            {
                var controllerName = api.ActionDescriptor.RouteValues["controller"];
                return new[] { controllerName };
            });

            // Configure operationId for better Postman import (uses method name as operationId)
            options.CustomOperationIds(apiDesc =>
            {
                if (apiDesc.ActionDescriptor is ControllerActionDescriptor actionDescriptor)
                {
                    // Use method name as operationId (e.g., "GetHomepageCategoriesWithProducts")
                    return actionDescriptor.ActionName;
                }
                return null;
            });

            // Include only controllers from this plugin in the frontend Swagger document
            // For other documents (like "v1" from Nop.Plugin.Api), return true to allow them
            options.DocInclusionPredicate((name, api) =>
            {
                // Only filter our own document "frontend-v1"
                if (string.Equals(name, "frontend-v1", StringComparison.OrdinalIgnoreCase))
                {
                    if (api.ActionDescriptor is ControllerActionDescriptor cad)
                    {
                        var ns = cad.ControllerTypeInfo.Namespace ?? string.Empty;
                        // Only our plugin's controllers in our document
                        return ns.StartsWith("Nop.Plugin.Misc.WebApi.Frontend.Controllers", StringComparison.Ordinal);
                    }
                    return false;
                }
                
                // For all other documents (e.g., "v1" from Nop.Plugin.Api), don't filter
                // Let them include their own controllers
                return true;
            });

            // Configure to handle [FromForm] parameters correctly
            options.OperationFilter<FileUploadOperationFilter>();
            
            // Apply camelCase to all parameter names (query, path, header parameters)
            options.OperationFilter<CamelCaseParameterFilter>();
            
            // Apply camelCase to all schema property names
            options.SchemaFilter<CamelCaseSchemaFilter>();
            
            // Map IFormFile to binary format
            options.MapType<IFormFile>(() => new OpenApiSchema
            {
                Type = "string",
                Format = "binary"
            });
        });

        services.AddSwaggerGenNewtonsoftSupport();
    }

    public void Configure(IApplicationBuilder app)
    {
        var environment = app.ApplicationServices.GetRequiredService<IWebHostEnvironment>();

        // Map all frontend API routes under /public-api/* for this plugin (controllers)
        app.MapWhen(context =>
        {
            var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
            return (path.StartsWith("/public-api/") || path.Equals("/public-api", StringComparison.OrdinalIgnoreCase))
                   && !path.StartsWith("/public-api/swagger", StringComparison.OrdinalIgnoreCase);
        }, branch =>
        {
            if (environment.IsDevelopment())
                branch.UseDeveloperExceptionPage();

            branch.UseRouting();
            branch.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        });

        // Separate branch for Swagger UI so URL is stable and не конфликтует с Nop.Plugin.Api
        app.MapWhen(context =>
        {
            var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
            return path.StartsWith("/public-api/swagger");
        }, branch =>
        {
            if (environment.IsDevelopment())
                branch.UseDeveloperExceptionPage();

            // JSON endpoint: /public-api/swagger/frontend-v1/swagger.json
            branch.UseSwagger(options =>
            {
                options.RouteTemplate = "public-api/swagger/{documentName}/swagger.json";
            });

            // UI: /public-api/swagger
            branch.UseSwaggerUI(c =>
            {
                c.RoutePrefix = "public-api/swagger";
                c.SwaggerEndpoint("/public-api/swagger/frontend-v1/swagger.json", "NopCommerce Frontend API v1");
                c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
                c.DisplayRequestDuration();
                c.EnableDeepLinking();
                c.EnableFilter();
            });
        });
    }

    // Run after core startup, order should be higher than default but before other plugins
    public int Order => 500;
}


