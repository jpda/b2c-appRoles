using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Graph;

namespace B2CAuthZ.Admin
{
    public interface IUserRepository
    {
        Task<ServiceResult<IEnumerable<User>>> GetUsers();
        Task<ServiceResult<User>> GetUser(string userId);
        Task<ServiceResult<User>> FindUserBySignInName(string userSignInName);
        Task<ServiceResult<IEnumerable<OrganizationUser>>> GetOrganizationUsers();
        Task<ServiceResult<OrganizationUser>> GetOrganizationUser(string userId);
        Task<ServiceResult<OrganizationUser>> FindOrganizationUserBySignInName(string userSignInName);
        Task<ServiceResult<IEnumerable<AppRoleAssignment>>> GetUserAppRoleAssignments(User u);
        Task<ServiceResult<IEnumerable<AppRoleAssignment>>> GetUserAppRoleAssignments(string userObjectId);
        Task<ServiceResult<OrganizationUser>> SetUserOrganization(OrganizationMembership membership);
        Task<ServiceResult<IEnumerable<OrganizationUser>>> SearchUser(string query);
    }
}