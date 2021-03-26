using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Graph;

namespace admin_func
{
    // public abstract class FilteredRepositoryFactory
    // {
    //     protected readonly GraphServiceClient _graphClient;
    //     protected FilteredRepositoryFactory(GraphServiceClient client)
    //     {
    //         this._graphClient = client;
    //     }

    //     protected abstract T CreateForOrgId<T>(string orgId);
    // }

    public interface IUserRepository
    {
        Task<IEnumerable<User>> GetUsers();
        Task<User> GetUser(string userId);
        Task<User> FindUserBySignInName(string userSignInName);
        Task<IEnumerable<AppRoleAssignment>> GetUserAppRoleAssignments(User u);
        Task<IEnumerable<AppRoleAssignment>> GetUserAppRoleAssignments(string userObjectId);
        Task<User> SetUserOrganization(OrganizationMembership membership);
    }
}