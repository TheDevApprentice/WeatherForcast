using Microsoft.AspNetCore.Authorization;

namespace application.Authorization
{
    /// <summary>
    /// Attribut pour vérifier une permission
    /// Usage: [HasPermission(AppClaims.ForecastWrite)]
    /// </summary>
    public class HasPermissionAttribute : AuthorizeAttribute
    {
        public HasPermissionAttribute(string permission)
        {
            Policy = permission;
        }
    }
}
