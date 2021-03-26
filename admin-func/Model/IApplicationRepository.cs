using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Graph;

namespace admin_func
{
    public interface IApplicationRepository
    {
        Task<AppRoleAssignment> AssignAppRole(string userId, string principalId, string resourceId, string appRoleId);
        Task<UserApplication> GetApplication(string userId, string resourceId);
        Task<IEnumerable<UserApplication>> GetApplications(string userId);
        Task<IEnumerable<AppRole>> GetAppRolesByServicePrincipal(string userId, string resourceId);
        Task<IEnumerable<AppRoleAssignment>> GetAppRoleAssignmentsByServicePrincipal(string userId, string servicePrincipalId);
    }
}