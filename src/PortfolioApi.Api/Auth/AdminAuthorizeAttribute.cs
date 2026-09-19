using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PortfolioApi.Application.Common;

namespace PortfolioApi.Api.Auth;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class AdminAuthorizeAttribute : Attribute, IAsyncAuthorizationFilter
{
    public const string HeaderName = "X-Admin-Key";

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (context.ActionDescriptor?.EndpointMetadata.OfType<AllowAnonymousAttribute>().Any() == true)
        {
            await Task.CompletedTask;
            return;
        }

        var validator = context.HttpContext.RequestServices.GetService(typeof(IAdminKeyValidator)) as IAdminKeyValidator;
        var key = context.HttpContext.Request.Headers[HeaderName].FirstOrDefault();

        if (validator is null || !validator.Validate(key))
        {
            context.Result = new UnauthorizedObjectResult(new { error = "Clave de administrador inválida." });
            return;
        }

        await Task.CompletedTask;
    }
}