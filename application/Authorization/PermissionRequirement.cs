using Microsoft.AspNetCore.Authorization;

namespace application.Authorization
{
    /// <summary>
    /// Requirement pour vérifier une permission
    /// </summary>
    public class PermissionRequirement : IAuthorizationRequirement
    {
        public string Permission { get; }

        public PermissionRequirement(string permission)
        {
            Permission = permission;
        }
    }
}
