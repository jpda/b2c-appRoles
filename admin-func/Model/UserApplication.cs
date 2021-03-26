using System.Collections.Generic;
using Microsoft.Graph;

namespace admin_func
{
    public class UserApplication
    {
        public string AppId { get; set; }
        public string ServicePrincipalId { get; set; }
        public IEnumerable<AppRoleAssignment> UserAssignedAppRoles { get; set; }
        public string DisplayName { get; set; }
    }
}