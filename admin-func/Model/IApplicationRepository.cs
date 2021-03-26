using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Graph;

namespace admin_func
{
    public interface IApplicationRepository
    {
        Task<AppRoleAssignment> AssignAppRole(string userId, string principalId, string resourceId, string appRoleId);
        Task<UserApplication> GetResource(string userId, string resourceId);
        Task<IEnumerable<UserApplication>> GetResources(string userId);
        Task<IEnumerable<AppRole>> GetAppRolesByResource(string userId, string resourceId);
        Task<IEnumerable<AppRoleAssignment>> GetAppRoleAssignmentsByResource(string userId, string resourceId);
    }
}