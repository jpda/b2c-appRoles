import React from "react";
import Table from "react-bootstrap/Table";
import Row from "react-bootstrap/Row";
import AuthService from "../auth/AuthService";
//import { AuthResponse, AuthenticationParameters } from "msal";
import { IKvp, Kvp } from "../../models/KvpModel";
import { Card, CardDeck } from "react-bootstrap";
import * as api from "../../api/api";

interface Props {
    auth: AuthService
    toastToggle: any;
    authenticationStateChanged: any;
}

interface State {
    userInfo: IKvp[];
    startDate: string;
    endDate: string;
}

export class OrganizationsView extends React.Component<Props, State> {
    state: State;
    auth: AuthService;
    scopeConfiguration: any;

    constructor(props: Props, state: State) {
        super(props, state);
        this.auth = props.auth;
        var now = new Date().toISOString();
        var weekFromNow = this.addDays(new Date(), 7).toISOString();
        this.state = { userInfo: [new Kvp("loading...", "loading...")], startDate: now, endDate: weekFromNow };

        // here we set the scopes we'll need to request from the user for this view
        this.scopeConfiguration = this.auth.config.requestConfiguration.scopes;
    }

    componentDidMount() {

    }

    fetchData() {

    }

    parseGraphResponse(data: any) {
        
        appsApi.vvApplicationsGet();
        //     if (data === null) {
        //         this.handleFatalError({ errorCode: "no_graph_response" });
        //     };
        //     var calData = [];
        //     for (var i = 0; i < data.value.length; i++) {
        //         calData.push(new Kvp(data.value[i].subject, data.value[i].start.dateTime));
        //     }

        //     this.setState({ userInfo: calData });
        // }

        // addDays(date: Date, days: number): Date {
        //     var result = new Date(date);
        //     result.setDate(result.getDate() + days);
        //     return result;
    }

    render() {
        return (
            <>
                <Row>
                    <CardDeck>
                        <Card>
                            <Card.Header as="h5">Single scope, statically assigned</Card.Header>
                            <Card.Body>
                                <p>In this example, the requested scopes are assigned in the application registration, before the application
                                ever runs. This is an administrative Azure AD activity, where the owner of the app registration
                                determines which scopes/permissions are required and enables the application to request them.
                                This is how Azure AD v1 resource permissions were handled.
                                </p>
                                <CardDeck>
                                    <Card>
                                        <Card.Body>
                                            <Card.Title>API/Service</Card.Title>
                                            <Card.Subtitle className="mb-2 text-muted">Microsoft Graph</Card.Subtitle>
                                        </Card.Body>
                                    </Card>
                                    <Card>
                                        <Card.Body>
                                            <Card.Title>Permission</Card.Title>
                                            <Card.Subtitle className="mb-2 text-muted">User.Read</Card.Subtitle>
                                        </Card.Body>
                                    </Card>
                                    <Card >
                                        <Card.Body>
                                            <Card.Title>Scope</Card.Title>
                                            <Card.Subtitle className="mb-2 text-muted"><code>https://graph.microsoft.com/User.Read</code></Card.Subtitle>
                                        </Card.Body>
                                    </Card>
                                    <Card >
                                        <Card.Body>
                                            <Card.Title>Assignment</Card.Title>
                                            <Card.Subtitle className="mb-2 text-muted">Static</Card.Subtitle>
                                        </Card.Body>
                                    </Card>
                                </CardDeck>
                            </Card.Body>
                        </Card>
                    </CardDeck>
                </Row>
                <Row>
                    <h2>Microsoft Graph data for /me/calendarview between {this.state.startDate} and {this.state.endDate}</h2>
                </Row>
                <Row>
                    <Table bordered striped>
                        <thead><tr><th>Key</th><th>Value</th></tr></thead>
                        <tbody>
                            {
                                this.state.userInfo.map((x, i) => {
                                    return <tr key={i}>
                                        <td>{x.key}</td>
                                        <td>{x.value}</td>
                                    </tr>
                                })
                            }
                        </tbody>
                    </Table>
                </Row>
            </>
        );
    }
}