using System.Collections.Generic;
using Microsoft.Graph;

namespace B2CAuthZ.Admin
{
    public class UserApplication
    {
        public string AppId { get; set; }
        public string ResourceId { get; set; }
        public IEnumerable<AppRoleAssignment> UserAssignedAppRoles { get; set; }
        public string DisplayName { get; set; }
    }
}