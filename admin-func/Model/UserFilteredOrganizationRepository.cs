using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Graph;
using System.Linq;

namespace admin_func
{
    // every query here is filtered by the calling user's orgId
    // orgId == custom user attribute on the user's object
    public class Organization
    {
        public string Id { get; set; }
        public IEnumerable<string> AllowedUserDomains { get; set; }
        public string DisplayName { get; set; }
    }

    public class OrganizationMembership
    {
        public string OrgId { get; set; }
        public string Role { get; set; }
    }

    public interface IOrganizationRepository
    {
        Task<Organization> GetOrganization(string organizationId, string orgId);
        Task<IEnumerable<Organization>> GetOrganizations(string userSignInName);
    }

    public class UserFilteredOrganizationRepository : IOrganizationRepository
    {
        // todo: move to options
        private const string tenantIssuerName = "jpdab2c.onmicrosoft.com";
        private const string orgIdExtension = "extension_bf169d2c7b1c4ebc9db1b9fd31964d98_OrgId";
        private readonly GraphServiceClient _graphClient;

        public UserFilteredOrganizationRepository(GraphServiceClient client)
        {
            _graphClient = client;
        }

        private async Task<List<OrganizationMembership>> GetUserOrganizations(string userSignInName)
        {
            // users/?$filter=identities/any(x:x/signInType eq 'emailAddress' and x/issuerAssignedId eq 'jdandison@gmail.com')
            var filter = new QueryOption(
                "$filter"
                , $"identities/any(x:x/issuer eq '{tenantIssuerName}' and x/issuerAssignedId eq '{userSignInName}')"
                );
            var userList = await _graphClient.Users
                .Request(new List<QueryOption>() { filter })
                .Select($"id,{orgIdExtension}")
                .GetAsync();

            if (!userList.Any()) throw new Exception("user not found");
            if (userList.Count > 1) throw new Exception("too many users");

            var user = userList.Single();
            if (!user.AdditionalData.Any()) throw new Exception("user doesn't have an orgid");

            if (user.AdditionalData.ContainsKey(orgIdExtension))
            {
                var orgData = user.AdditionalData[orgIdExtension].ToString();
                return System.Text.Json.JsonSerializer.Deserialize<List<OrganizationMembership>>(orgData);
            }
            throw new Exception("user has no org id or malformed");
        }

        private async Task<List<OrganizationMembership>> GetUserOrganizations(Guid userObjectId)
        {
            var user = await _graphClient.Users[userObjectId.ToString()]
                .Request()
                .Select($"id,{orgIdExtension}")
                .GetAsync();

            if (!user.AdditionalData.Any()) throw new Exception("user doesn't have an orgid");

            if (user.AdditionalData.ContainsKey(orgIdExtension))
            {
                var orgData = user.AdditionalData[orgIdExtension].ToString();
                return System.Text.Json.JsonSerializer.Deserialize<List<OrganizationMembership>>(orgData);
            }
            throw new Exception("user has no org id or malformed");
        }

        public async Task<Organization> GetOrganization(string userSignInName, string orgId)
        {
            var userOrgId = await this.GetUserOrganizations(userSignInName);
            // todo: figure out organization repository - tables? probably
            return new Organization() { Id = userOrgId.FirstOrDefault(x => x.OrgId == orgId).OrgId };
        }

        public async Task<IEnumerable<Organization>> GetOrganizations(string userSignInName)
        {
            var userOrgId = await this.GetUserOrganizations(userSignInName);
            // todo: figure out organization repository - tables? probably
            return userOrgId.Select(x => new Organization() { Id = x.OrgId });
        }
    }
}