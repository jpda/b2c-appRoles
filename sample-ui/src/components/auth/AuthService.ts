import * as msal from "@azure/msal-browser";
import { IMsalHandlerConfiguration, DefaultMsalHandlerConfiguration } from "./MsalConfig";

export class UserInfo {
    accountAvailable: boolean;
    displayName: string;
    constructor() {
        this.displayName = "";
        this.accountAvailable = false;
    }
}

export default class MsalHandler {
    msalObj: msal.PublicClientApplication;
    config: IMsalHandlerConfiguration;

    // for handling a single instance of the handler, use getInstance() elsewhere
    static instance: MsalHandler;
    private static createInstance() {
        var a = new MsalHandler();
        return a;
    }

    public static getInstance() {
        if (!this.instance) {
            this.instance = this.createInstance();
        }
        return this.instance;
    }

    // public static getInstanceWithExternalConfiguration(config: IMsalHandlerConfiguration) {

    // }

    // we want this private to prevent any external callers from directly instantiating, instead rely on getInstance()
    private constructor() {
        this.config = new DefaultMsalHandlerConfiguration();
        this.track("ctor: starting");
        const a = new msal.PublicClientApplication(this.config.config);

        this.track("ctor: setting redirect callbacks");
        a.handleRedirectPromise().then((tokenResponse) => {
            if (tokenResponse) {
                this.processLogin(tokenResponse);
            }
        }).catch((error) => {
            console.error(error);
        });
        this.msalObj = a;
    }

    public async login(redirect?: boolean, state?: string, scopes?: string[]) {
        this.track("entering login; scopes: " + scopes + ", state: " + state + ", redirect: " + redirect);
        if (state) {
            this.track("Setting state to: " + state);
            this.config.requestConfiguration.state = JSON.stringify({ appState: true, state });
        }
        if (redirect || this.config.redirect) {
            this.track("redirecting to login with parameters: " + JSON.stringify(this.config.requestConfiguration));
            this.msalObj.loginRedirect(this.config.requestConfiguration);
        } else {
            try {
                this.track("logging in with popup, config: " + JSON.stringify(this.config.requestConfiguration));
                var response = await this.msalObj.loginPopup(this.config.requestConfiguration);
                this.track("MsalHandler::login: got something: " + JSON.stringify(response));
                this.processLogin(response);
            } catch (e) {
                console.error(e);
            }
        }
    }

    public async acquireAccessToken(state?: string, redirect?: boolean, scopes?: string[]): Promise<String | null> {
        var requestScopes = scopes ? scopes : this.config.requestConfiguration.scopes;
        if (state) {
            this.track("state: " + state);
            this.config.requestConfiguration.state = JSON.stringify({ appState: true, state });
        }
        try {
            this.track("access token silent: " + JSON.stringify(this.config.requestConfiguration));
            var token = await this.msalObj.acquireTokenSilent({ scopes: requestScopes });
            return token.accessToken;
        } catch (e) {
            if (e instanceof msal.AuthError) {
                console.error("acquireAccessToken: error: " + JSON.stringify(e));
                if (e.errorCode === "user_login_error"
                    || e.errorCode === "consent_required"
                    || e.errorCode === "interaction_required") { // todo: check for other error codes
                    this.login(redirect, state, requestScopes);
                }
            }
            console.error(e);
        }
        return null;
    }

    public getUserData(): UserInfo {
        var account = this.msalObj.getActiveAccount();
        var u = new UserInfo();
        if (account) {
            u.accountAvailable = true;
            u.displayName = account.name;
        }
        return u;
    }

    public processLogin(response: msal.AuthenticationResult | undefined) {
        this.track("processLogin");
        if (!response) return;
        this.track("id_token received: " + response.idToken);
        this.track("access_token received: " + response.accessToken);
        this.track("state received: " + response.state);
        if (response.state) {
            this.track("got a " + response.state);
            try {
                var state = JSON.parse(response.state);
                if (state.appState) { // we had a redirect from another place in the app before the authentication request
                    this.track("got state, and it's ours: " + state.state);
                    window.location.pathname = state.state;
                }
            } catch {
                this.track("couldn't parse state - maybe not ours");
            }
        }
    }

    private track(message: string) {
        // lol: this is ridiculous - make sure you turn this off with this.useStackLogging = false
        var msg = "MsalHandler::" + message;
        if (this.config.piiStackLogging) {
            var e = new Error("ok");
            var stack = e.stack?.split("\n")[2].trim();
            var start = stack?.indexOf("(");
            var prefix = msg?.substring(3, start).trim();
            console.debug(prefix + message);
        }
        else {
            console.log(msg);
        }
    }
}