namespace B2CAuthZ.Admin.WebApiHost
{
    public class OrganizationOptions
    {
        private string _userFieldSelection;
        public string TenantIssuerName { get; set; }
        public string OrgIdClaimName { get; set; }
        public string OrgIdExtensionName { get; set; }
        public string OrgRoleExtensionName { get; set; }
        public string UserFieldSelection
        {
            get => $"{_userFieldSelection},{OrgIdExtensionName},{OrgRoleExtensionName}";
            set { _userFieldSelection = value; }
        }
        public string ApplicationOrgAdministratorRoleId { get; set; }
        public string AppRoleAssignmentFieldSelection { get; set; }
    }
}