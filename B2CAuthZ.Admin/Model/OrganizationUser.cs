using System.Collections.Generic;
using System.Linq;

namespace B2CAuthZ.Admin
{
    public class OrganizationUser
    {
        public OrganizationUser() { }

        internal OrganizationUser(Microsoft.Graph.User u, string orgIdExtension, string orgRoleExtension, string tenantIssuerName)
        {
            this.Id = u.Id;
            this.DisplayName = u.DisplayName;
            this.GivenName = u.GivenName;
            this.Surname = u.Surname;
            this.UserPrincipalName = u.UserPrincipalName;

            if (u.AdditionalData == null || !u.AdditionalData.Any()) return;
            if (u.AdditionalData.ContainsKey(orgIdExtension))
            {
                this.OrgId = u.AdditionalData[orgIdExtension].ToString();
            }
            if (u.AdditionalData.ContainsKey(orgRoleExtension))
            {
                this.OrgRole = u.AdditionalData[orgRoleExtension].ToString();
            }
            if (u.Identities == null || !u.Identities.Any()) return;
            SignInNames = u.Identities.Where(x => x.Issuer == tenantIssuerName && x.SignInType != "userPrincipalName").Select(x => x.IssuerAssignedId);
            Identities = u.Identities;
        }

        internal OrganizationUser(Microsoft.Graph.User u, OrganizationOptions config) : this(u, config.OrgIdExtensionName, config.OrgRoleExtensionName, config.TenantIssuerName) { }

        public string Id { get; set; }
        public string DisplayName { get; set; }
        public string GivenName { get; set; }
        public string Surname { get; set; }
        public string UserPrincipalName { get; set; }
        public string OrgId { get; set; }
        public string OrgRole { get; set; }
        public IEnumerable<string> SignInNames { get; set; }
        public IEnumerable<Microsoft.Graph.ObjectIdentity> Identities { get; set; }
    }
}