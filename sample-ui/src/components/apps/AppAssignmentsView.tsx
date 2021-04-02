import React from "react";
import Table from "react-bootstrap/Table";
import Row from "react-bootstrap/Row";
import { Card, CardDeck } from "react-bootstrap";
import { AppRoleAssignment, UserApplication } from "../../api/interfaces";
import { APIClient } from "../../api/APIClient";

interface Props {
    apiClient: APIClient;
    resourceId: string;
}

interface State {
    appInfo: UserApplication | undefined;
    assignments: AppRoleAssignment[];
}

export class AppAssignmentsView extends React.Component<Props, State> {
    apiClient: APIClient;
    state: State;
    scopeConfiguration: any;
    resourceId: string;

    constructor(props: Props, state: State) {
        super(props, state);
        this.resourceId = props.resourceId;
        this.state = { appInfo: undefined, assignments: [] };
        this.apiClient = props.apiClient;
    }

    async componentDidMount() {
        var appsService = this.apiClient.rest.v1_0.servicePrincipalsService;
        var assignmentService = this.apiClient.rest.v1_0.serviceprincipals.appRoleAssignedToService;
        var app = await appsService.getByResourceId(this.resourceId);
        var assignments = await assignmentService.getByServicePrincipalId(this.resourceId);
        this.setState({ appInfo: app, assignments: assignments });
    }

    render() {
        return (
            <>
                <Row>
                    <CardDeck>
                        <Card>
                            <Card.Header as="h5">Apps</Card.Header>
                            <Card.Body>
                                <p>
                                    These are applications you can manage.
                                </p>
                                <CardDeck>
                                    <Card>
                                        <Card.Body>
                                            <Card.Title>API/Service</Card.Title>
                                            <Card.Subtitle className="mb-2 text-muted">Microsoft Graph</Card.Subtitle>
                                        </Card.Body>
                                    </Card>
                                </CardDeck>
                            </Card.Body>
                        </Card>
                    </CardDeck>
                </Row>
                <Row>
                    <h2>Assignments</h2>
                </Row>
                <Row>
                    <Table bordered striped>
                        <thead><tr><th>Resource ID</th><th>Name</th></tr></thead>
                        <tbody>
                            {
                                <tr>
                                    <td>{this.state.appInfo?.resourceId}</td>
                                    <td>{this.state.appInfo?.displayName}</td>
                                </tr>
                            }
                        </tbody>
                    </Table>
                </Row>
                <Row>
                    <Table bordered striped>
                        <thead>
                            <tr>
                                <th>App Role ID</th>
                                <th>Name</th>
                                <th>Principal ID</th>
                                {/* <th>Resource ID</th>
                                <th>Resource Name</th> */}
                            </tr>
                        </thead>
                        <tbody>
                            {
                                this.state.assignments.map((x, i) => {
                                    return <tr key={i}>
                                        <td>{x.appRoleId}</td>
                                        <td>{x.principalDisplayName}</td>
                                        <td>{x.principalId}</td>
                                        {/* <td>{x.resourceId}</td>
                                        <td>{x.resourceDisplayName}</td> */}
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