using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Graph;
using System.Linq;

namespace admin_func
{
    public class UserRepositoryFactory// : FilteredRepositoryFactory
    {
        private readonly GraphServiceClient _graphClient;
        public UserRepositoryFactory(GraphServiceClient client)// : base(client)
        {
            _graphClient = client;
        }
        public IUserRepository CreateForOrgId(string orgId)
        {
            return new OrganizationFilteredUserRepository(_graphClient, orgId);
        }
    }

    public class OrganizationFilteredUserRepository : FilteredRepository, IUserRepository
    {
        public OrganizationFilteredUserRepository(GraphServiceClient client, string orgId) : base(client, orgId) { }

        public async Task<Microsoft.Graph.User> GetUser(string userId)
        {
            var user = await _graphClient.Users[userId]
                .Request()
                .Select(userFieldSelection)
                .GetAsync();

            // todo: wrap this in ServiceResult or similar
            if (!user.AdditionalData.Any()) return null;
            if (user.AdditionalData.ContainsKey(orgIdExtension))
            {
                var orgData = user.AdditionalData[orgIdExtension].ToString();
                if (string.Equals(orgData, _orgId, StringComparison.OrdinalIgnoreCase))
                {
                    return user;
                }
            }
            return null;
        }

        public async Task<User> FindUserBySignInName(string userSignInName)
        {
            var filter = new QueryOption(
                "$filter"
                , $"identities/any(x:x/issuer eq '{tenantIssuerName}' and x/issuerAssignedId eq '{userSignInName}')"
                );
            var userList = await _graphClient.Users
                .Request(new List<QueryOption>() { filter })
                .Select(userFieldSelection)
                .GetAsync();

            if (!userList.Any()) throw new Exception("user not found");
            if (userList.Count > 1) throw new Exception("too many users");

            var user = userList.Single();
            if (!user.AdditionalData.Any()) throw new Exception("user doesn't have an orgid");

            if (user.AdditionalData.ContainsKey(orgIdExtension))
            {
                var orgData = user.AdditionalData[orgIdExtension].ToString();
                if (string.Equals(orgData, _orgId, StringComparison.OrdinalIgnoreCase))
                {
                    return user;
                }
            }
            throw new Exception("user has no org id or malformed");
        }

        public async Task<IEnumerable<Microsoft.Graph.User>> GetUsers()
        {
            var filter = new QueryOption("$filter", $"{orgIdExtension} eq '{_orgId}'");
            var users = await _graphClient.Users
                .Request(new List<QueryOption>() { filter })
                .Select(userFieldSelection)
                .GetAsync();
            return users;
        }

        public async Task<IEnumerable<Microsoft.Graph.AppRoleAssignment>> GetUserAppRoleAssignments(User u)
        {
            return await this.GetUserAppRoleAssignments(u.Id);
        }
        public async Task<IEnumerable<Microsoft.Graph.AppRoleAssignment>> GetUserAppRoleAssignments(string userObjectId)
        {
            var user = await _graphClient.Users[userObjectId]
                .Request()
                .Select(userFieldSelection)
                .GetAsync();

            if (!user.AdditionalData.Any()) return null;
            if (user.AdditionalData.ContainsKey(orgIdExtension))
            {
                var orgData = user.AdditionalData[orgIdExtension].ToString();
                if (string.Equals(orgData, _orgId, StringComparison.OrdinalIgnoreCase))
                {
                    return await _graphClient.Users[userObjectId].AppRoleAssignments
                        .Request()
                        .GetAsync();
                }
            }
            return new List<AppRoleAssignment>();
        }
    }
}