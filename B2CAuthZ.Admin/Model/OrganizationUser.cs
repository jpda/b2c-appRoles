using System.Linq;

namespace B2CAuthZ.Admin
{
    public class OrganizationUser
    {
        public OrganizationUser() { }

        internal OrganizationUser(Microsoft.Graph.User u, string orgIdExtension, string orgRoleExtension)
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
        }

        public string Id { get; set; }
        public string DisplayName { get; set; }
        public string GivenName { get; set; }
        public string Surname { get; set; }
        public string UserPrincipalName { get; set; }
        public string OrgId { get; set; }
        public string OrgRole { get; set; }
    }
}