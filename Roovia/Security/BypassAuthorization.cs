using Microsoft.AspNetCore.Authorization;

namespace Roovia.Security
{
    /// <summary>
    /// Attribute to bypass authorization for system-level operations
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class BypassAuthorizationAttribute : Attribute
    {
    }

    /// <summary>
    /// Authorization handler that checks for the BypassAuthorization attribute
    /// </summary>
    public class BypassAuthorizationHandler : IAuthorizationHandler
    {
        public Task HandleAsync(AuthorizationHandlerContext context)
        {
            // Check if the endpoint has the BypassAuthorization attribute
            var endpoint = context.Resource as Microsoft.AspNetCore.Http.Endpoint;
            if (endpoint?.Metadata.GetMetadata<BypassAuthorizationAttribute>() != null)
            {
                // Bypass all requirements
                foreach (var requirement in context.PendingRequirements.ToList())
                {
                    context.Succeed(requirement);
                }
            }

            return Task.CompletedTask;
        }
    }
}