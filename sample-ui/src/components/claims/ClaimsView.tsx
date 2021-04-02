import React from "react";
import { IClaim, Claim } from "../../models/ClaimModel";
import Table from "react-bootstrap/Table";
import Row from "react-bootstrap/Row";
import MsalHandler from "../auth/MsalHandler";

interface Props {
    auth: MsalHandler;
}

interface State {
    claims: IClaim[]
}

export default class ClaimsView extends React.Component<Props, State> {
    auth: MsalHandler;
    data: Props;

    constructor(props: Props) {
        super(props);
        this.data = props;
        this.auth = props.auth;
        this.state = { claims: [] };
    }

    parseClaims(token: any) {
        console.log(token);
        var claimData = Object.keys(token).filter(y => y !== "decodedIdToken" && y !== "rawIdToken").map(x => {
            return new Claim(x, Array.isArray(token[x]) ? token[x].join(",") : token[x].toString());
        });
        this.setState({ claims: claimData });
    }

    async componentDidMount() {
        var account = this.auth.msalObj.getActiveAccount();
        if (account) {
            this.parseClaims(account.idTokenClaims);
        } else {
            try {
                var login = await this.auth.login();
                this.parseClaims(login?.idTokenClaims);
            } catch (error) {
                console.error(error);
                console.error(error.errorCode);
                await this.auth.login();
            }
        }
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