import React from "react";
import Table from "react-bootstrap/Table";
import Row from "react-bootstrap/Row";
import { Card, CardDeck } from "react-bootstrap";
import { UserApplication } from "../../api/interfaces";
import { APIClient } from "../../api/APIClient";
import { Link } from "react-router-dom";

interface Props {
    apiClient: APIClient;
}

interface State {
    appInfo: UserApplication[];
}

export class AppsView extends React.Component<Props, State> {
    apiClient: APIClient;
    state: State;
    scopeConfiguration: any;

    constructor(props: Props, state: State) {
        super(props, state);
        this.state = { appInfo: [] };
        this.apiClient = props.apiClient;
    }

    async componentDidMount() {
        var appsService = this.apiClient.rest.v1_0.applicationsService;
        var apps = await appsService.getAll();
        this.setState({ appInfo: apps });
    }

    render() {
        return (
            <>
                <Row>
                    <CardDeck>
                        <Card>
                            <Card.Header as="h5">Apps you have access to manage</Card.Header>
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
                    <h2>Applications</h2>
                </Row>
                <Row>
                    <Table bordered striped>
                        <thead><tr><th>Name</th><th>Resource ID</th></tr></thead>
                        <tbody>
                            {
                                this.state.appInfo.map((x, i) => {
                                    return <tr key={i}>
                                        <td>
                                            <Link to={`/apps/${x.resourceId}/assignments`}>{x.displayName}</Link>
                                        </td>
                                        <td>{x.resourceId}</td>
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