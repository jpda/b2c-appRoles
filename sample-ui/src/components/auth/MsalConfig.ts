export interface IMsalConfig {
    // b2c configuration
    auth: {
        clientId: string,
        authority: string,
        redirectUri: string,
        navigateToLoginRequestUrl: boolean,
        validateAuthority: boolean,

    },
    cache: {
        cacheLocation: string
    }
}

export interface IMsalHandlerConfiguration {
    config: IMsalConfig;
    requestConfiguration: IRequestConfiguration;
    redirect: boolean;
    piiStackLogging: boolean;
}

export interface IRequestConfiguration {
    scopes: string[];
    state?: string;
}

export class DefaultMsalHandlerConfiguration implements IMsalHandlerConfiguration {
    config: IMsalConfig;
    requestConfiguration: IRequestConfiguration;
    redirect: boolean;
    piiStackLogging: boolean;

    constructor() {
        this.config = {
            // b2c configuration
            auth: {
                clientId: "257d42b5-3a75-4ffe-9057-7ec6bdc4d2b4",
                authority: "https://jpdab2c.b2clogin.com/jpdab2c.onmicrosoft.com/B2C_1_susi_rec",
                redirectUri: "http://localhost:3000/",
                navigateToLoginRequestUrl: false,
                validateAuthority: false
            },
            cache: {
                cacheLocation: "sessionStorage" // session storage is more secure, but prevents single-sign-on from working. other option is 'localStorage'
            }
        };
        this.piiStackLogging = true;
        this.redirect = false;
        this.requestConfiguration = {
            // b2c api scopes
            scopes: ["https://jpdab2c.onmicrosoft.com/authz-admin/Access"]
        }
    }
}