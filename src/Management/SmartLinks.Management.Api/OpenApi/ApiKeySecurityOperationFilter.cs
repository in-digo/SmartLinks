using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SmartLinks.Management.Api.OpenApi;

/// <summary>
/// Добавляет требование API-key к защищённым операциям OpenAPI
/// </summary>
public sealed class ApiKeySecurityOperationFilter : IOperationFilter
{
    private const string _apiKeySecurityScheme = "ApiKey";

    /// <summary>
    /// Добавляет security requirement при наличии метаданных авторизации
    /// </summary>
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var requiresAuthorization = context.ApiDescription
            .ActionDescriptor
            .EndpointMetadata
            .OfType<IAuthorizeData>()
            .Any();

        if (!requiresAuthorization)
            return;

        operation.Security ??= [];

        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = _apiKeySecurityScheme
                }
            }] = Array.Empty<string>()
        });
    }
}