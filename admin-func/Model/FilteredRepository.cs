using Microsoft.Graph;

namespace admin_func
{
    // public class FilteredRepositoryFactory
    // {
    //     private readonly GraphServiceClient _graphClient;
    //     public FilteredRepositoryFactory(GraphServiceClient client)
    //     {
    //         _graphClient = client;
    //     }
    //     public T CreateForOrgId<T>(string orgId) where T : FilteredRepository
    //     {
    //         return Activator.CreateInstance<T>(new object[] { _graphClient, orgId });
    //     }
    // }

    public abstract class FilteredRepository
    {
        // todo: move to config/ioptions
        protected const string tenantIssuerName = "jpdab2c.onmicrosoft.com";
        protected const string orgIdExtension = "extension_bf169d2c7b1c4ebc9db1b9fd31964d98_OrgId";
        protected const string orgRoleExtension = "extension_bf169d2c7b1c4ebc9db1b9fd31964d98_OrgRole";
        // todo: move this
        protected readonly string userFieldSelection = $"id,userPrincipalName,displayName,givenName,surname,{orgIdExtension},{orgRoleExtension}";
        protected readonly GraphServiceClient _graphClient;
        protected readonly string _orgId;

        protected FilteredRepository(GraphServiceClient client, string orgId)
        {
            _graphClient = client;
            _orgId = orgId;
        }
    }
}