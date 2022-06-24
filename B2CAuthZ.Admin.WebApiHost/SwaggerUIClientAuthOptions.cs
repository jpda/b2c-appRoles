namespace B2CAuthZ.Admin.WebApiHost
{
    //  "Instance": "https://login.b2x.studio/",
    // "ClientId": "ff2c41c7-0b28-46bc-b5e7-b810a3ae99b5",
    // "Domain": "b2x.studio",
    // "SignUpSignInPolicyId": "b2c_1a_08-rp-hrd_signin"
    public class SwaggerUIClientAuthOptions
    {
        public string Instance { get; set; }
        public string ClientId { get; set; }
        public string Domain { get; set; }
        public string Scopes { get; set; }
        public string SignUpSignInPolicyId { get; set; }
        public string AuthorizationUrl => $"{Instance}{Domain}/{SignUpSignInPolicyId}/oauth2/v2.0/authorize";
        public string TokenUrl => $"{Instance}{Domain}/{SignUpSignInPolicyId}/oauth2/v2.0/token";
    }
}