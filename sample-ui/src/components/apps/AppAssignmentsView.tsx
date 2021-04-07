import React from "react";
import Table from "react-bootstrap/Table";
import Row from "react-bootstrap/Row";
import { MdClose } from "react-icons/md";
import { Button, ButtonGroup, ButtonToolbar, Card } from "react-bootstrap";
import { AppRoleAssignment, UserApplication, AppRole } from "../../api/interfaces";
import { APIClient } from "../../api/APIClient";

interface Props {
    apiClient: APIClient;
    resourceId: string;
}

interface State {
    appInfo: UserApplication | undefined;
    assignments: AppRoleAssignment[];
    appRoles: AppRole[];
}

export class AppAssignmentsView extends React.Component<Props, State> {
    apiClient: APIClient;
    state: State;
    scopeConfiguration: any;
    resourceId: string;
    appRoles: AppRole[];

    constructor(props: Props, state: State) {
        super(props, state);
        this.resourceId = props.resourceId;
        this.state = { appInfo: undefined, assignments: [], appRoles: [] };
        this.apiClient = props.apiClient;
        this.appRoles = [];
    }

    async componentDidMount() {
        var appsService = this.apiClient.rest.v1_0.servicePrincipalsService;
        var assignmentService = this.apiClient.rest.v1_0.serviceprincipals.appRoleAssignedToService;
        var app = await appsService.getByResourceId(this.resourceId);
        var assignments = await assignmentService.getByResourceId(this.resourceId);
        this.setState({ appInfo: app, assignments: assignments, appRoles: [] });
        await this.resolveAppRoleName();
    }

    async resolveAppRoleName() {
        var appRoleService = this.apiClient.rest.v1_0.serviceprincipals.appRolesService;
        var resourceAppRoles = await appRoleService.getByResourceId(this.resourceId);
        var replacedNames = this.state.assignments.map((x) => {
            if (resourceAppRoles !== undefined) {
                var name = resourceAppRoles.find(y => y.id === x.appRoleId)?.value;
                x.appRoleId = name;
            }
            return x;
        });
        this.setState({ appInfo: this.state.appInfo, assignments: replacedNames, appRoles: resourceAppRoles });
    }

    async addAssignment(assignment: AppRoleAssignment) {
        var appsService = this.apiClient.rest.v1_0.serviceprincipals.appRoleAssignedToService;
        await appsService.postByResourceId(this.resourceId, assignment);
    }

    async removeAssignment(assignmentId: string) {
        var appsService = this.apiClient.rest.v1_0.servicePrincipalsService;
        await appsService.deleteByResourceId(this.resourceId, assignmentId);
    }

    render() {
        return (
            <>
                <Row>
                    <Card>
                        <Card.Header as="h5">Apps</Card.Header>
                        <Card.Body>
                            <p>Add app role assignments to users here.</p>
                        </Card.Body>
                    </Card>
                </Row>
                <Row>
                    <h2>App role assignments for {this.state.appInfo?.displayName}</h2>
                </Row>
                <Row>
                    <ButtonToolbar aria-label="Toolbar with button groups">
                        <ButtonGroup className="mr-2" aria-label="First group">
                            <Button href={`/apps/${this.resourceId}/assignments/add`}>Add assignment</Button>
                        </ButtonGroup>
                        <ButtonGroup className="mr-2" aria-label="Second group">
                            <Button>5</Button> <Button>6</Button> <Button>7</Button>
                        </ButtonGroup>
                        <ButtonGroup aria-label="Third group">
                            <Button>8</Button>
                        </ButtonGroup>
                    </ButtonToolbar>
                </Row>
                <Row>
                    <Table bordered striped>
                        <thead>
                            <tr>
                                <th>App Role</th>
                                <th>User name</th>
                                <th>Principal</th>
                                <th>Actions</th>
                            </tr>
                        </thead>
                        <tbody>
                            {
                                this.state.assignments.map((x, i) => {
                                    return <tr key={i}>
                                        <td>{x.appRoleId}</td>
                                        <td>{x.principalDisplayName}</td>
                                        <td>{x.principalId}</td>
                                        <td>
                                            <ButtonGroup className="mr-2" aria-label="First group">
                                                <Button variant="danger" onClick={() => this.removeAssignment(x.id ? x.id : "id missing")}><MdClose /> Remove </Button>
                                            </ButtonGroup>
                                        </td>
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