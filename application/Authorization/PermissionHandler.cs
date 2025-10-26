using domain.Constants;
using Microsoft.AspNetCore.Authorization;

namespace application.Authorization
{
    /// <summary>
    /// Handler pour vérifier les permissions
    /// </summary>
    public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            PermissionRequirement requirement)
        {
            // Vérifier si l'utilisateur a le claim de permission
            if (context.User.HasClaim(c => 
                c.Type == AppClaims.Permission && 
                c.Value == requirement.Permission))
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}
