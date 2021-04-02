import React from "react";
import Table from "react-bootstrap/Table";
import Row from "react-bootstrap/Row";
import { Card, CardDeck } from "react-bootstrap";
import { OrganizationUser } from "../../api/interfaces";
import { APIClient } from "../../api/APIClient";

interface Props {
    apiClient: APIClient;
}

interface State {
    userInfo: OrganizationUser[];
}

export class UsersView extends React.Component<Props, State> {
    apiClient: APIClient;
    state: State;
    scopeConfiguration: any;

    constructor(props: Props, state: State) {
        super(props, state);
        this.state = { userInfo: [] };
        this.apiClient = props.apiClient;
    }

    async componentDidMount() {
        var usersService = this.apiClient.rest.v1_0.usersService;
        var users = await usersService.getAll();
        this.setState({ userInfo: users });
    }

    render() {
        return (
            <>
                <Row>
                    <CardDeck>
                        <Card>
                            <Card.Header as="h5">Organizational users</Card.Header>
                            <Card.Body>
                                <p>
                                    These users are members of your organization. Membership is driven by the OrgId attribute on the user's object.
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
                    <h2>Users</h2>
                </Row>
                <Row>
                    <Table bordered striped>
                        <thead><tr><th>Key</th><th>Value</th></tr></thead>
                        <tbody>
                            {
                                this.state.userInfo.map((x, i) => {
                                    return <tr key={i}>
                                        <td>{x.displayName}</td>
                                        <td>{x.id}</td>
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