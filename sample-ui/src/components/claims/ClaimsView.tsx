import React from "react";
import { IClaim, Claim } from "../../models/ClaimModel";
import Table from "react-bootstrap/Table";
import Row from "react-bootstrap/Row";
import AuthService from "../auth/AuthService";

interface Props {
    auth: AuthService;
    toastToggle: any;
}

interface State {
    claims: IClaim[]
}

export default class ClaimsView extends React.Component<Props, State> {
    auth: AuthService;
    data: Props;

    constructor(props: Props) {
        super(props);
        this.data = props;
        this.auth = props.auth;
        this.state = { claims: [] };
    }

    parseToken(token: any) {
        var claimData = Object.keys(token).filter(y => y !== "decodedIdToken" && y !== "rawIdToken").map(x => {
            return new Claim(x, Array.isArray(token[x]) ? token[x].join(",") : token[x].toString());
        });
        this.setState({ claims: claimData });
    }

    componentDidMount() {
        this.handleData();
    }

    //todo: add toggle between id_token and access_tokens
    handleData() {
        if (this.auth.msalObj.getActiveAccount()) {
            this.parseToken(this.auth.msalObj.getActiveAccount()?.idTokenClaims);
        } else {
            this.auth.msalObj.loginPopup(this.auth.data.requestConfig).then(token => this.parseToken(token.idToken)).catch(e => { this.tokenError(e) });
        }
    }

    tokenError(e: any) {
        console.error(e);
        console.error(e.errorCode);
        if (e.errorCode === "user_login_error") {
            this.auth.msalObj.loginPopup(this.auth.config).then(token => { this.parseToken(token.idToken) }).catch(e => { this.showError(e); });
        } else {
            this.showError(e);
        }
    }

    showError(e: any) {
        this.props.toastToggle(true, e.errorCode);
    }

    render() {
        return (
            <Row>
                <Table bordered striped>
                    <thead><tr><th>Name</th><th>Value</th></tr></thead>
                    <tbody>
                        {
                            this.state.claims.map((x, i) => {
                                return <tr key={i}><td>{x.key}</td><td>{x.value}</td></tr>
                            })
                        }
                    </tbody>
                </Table>
            </Row>
        );
    }
}