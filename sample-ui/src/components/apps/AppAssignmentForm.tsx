import React from "react";
import Row from "react-bootstrap/Row";
import { Button, Form } from "react-bootstrap";
import { AppRoleAssignment, UserApplication, AppRole, OrganizationUser } from "../../api/interfaces";
import { APIClient } from "../../api/APIClient";

interface Props {
    apiClient: APIClient;
    resourceId: string;
}

interface State {
    appInfo: UserApplication;
    appRoles: AppRole[];
    user: OrganizationUser;
    query: string;
    selectedRoleId: string;
}

export class AppAssignmentForm extends React.Component<Props, State> {
    apiClient: APIClient;
    state: State;
    scopeConfiguration: any;
    resourceId: string;

    constructor(props: Props, state: State) {
        super(props, state);
        this.resourceId = props.resourceId;
        this.state = { appInfo: {}, appRoles: [], user: {}, query: "", selectedRoleId: "" };
        this.apiClient = props.apiClient;
    }

    async componentDidMount() {
        var appRoleService = this.apiClient.rest.v1_0.serviceprincipals.appRolesService;
        var resourceAppRoles = await appRoleService.getByResourceId(this.resourceId);
        // var replacedNames = this.state.assignments.map((x) => {
        //     if (resourceAppRoles !== undefined) {
        //         var name = resourceAppRoles.find(y => y.id === x.appRoleId)?.value;
        //         x.appRoleId = name;
        //     }
        //     return x;
        // });
        this.setState({ appInfo: this.state.appInfo, appRoles: resourceAppRoles });
    }

    async addAssignment(event: any) {
        event.preventDefault();
        alert(JSON.stringify(event.target.value));
        var assignment: AppRoleAssignment = {
            appRoleId: this.state.selectedRoleId,
            principalId: this.state.user.id,
            resourceId: this.resourceId
        };
        alert(JSON.stringify(assignment));
        var appsService = this.apiClient.rest.v1_0.serviceprincipals.appRoleAssignedToService;
        //await appsService.postByResourceId(this.resourceId, assignment);
    }

    async removeAssignment(assignmentId: string) {
        var appsService = this.apiClient.rest.v1_0.servicePrincipalsService;
        await appsService.deleteByResourceId(this.resourceId, assignmentId);
    }

    async resolveUser(event: any) {
        var data = event.target.value;
        this.setState({ query: data });
        if (data.length < 3) return;
        var userService = this.apiClient.rest.v1_0.users.searchService;
        console.log(`resolving ${data}`);
        var foundUser = await userService.getByQuery(data);
        this.setState({ user: foundUser });
    }

    setSelectedRole(event: any){
        this.setState({selectedRoleId: event.target.value});
    }

    render() {
        return (
            <>
                <Row>
                    <h2>Add app role assignment for {this.state.appInfo.displayName}</h2>
                </Row>
                <Row>
                    <h3>{this.state.user.displayName} {this.state.user.userPrincipalName}</h3>
                </Row>
                <Row>
                    <Form onSubmit={this.addAssignment.bind(this)}>
                        <Form.Group controlId="userSearch">
                            <Form.Label>User</Form.Label>
                            <Form.Control type="string" placeholder="Enter email" defaultValue={this.state.query} value={this.state.user.displayName} onBlur={this.resolveUser.bind(this)} />
                            <Form.Text className="text-muted">Enter a username or email</Form.Text>
                        </Form.Group>
                        <Form.Group controlId="userSelectedRole">
                            <Form.Label>Role</Form.Label>
                            <Form.Control as="select" onChange={this.setSelectedRole.bind(this)} placeholder="Choose a role...">
                                {
                                    this.state.appRoles.map((x, i) => {
                                        return <option key={i} value={x.id}>{x.displayName}</option>;
                                    })
                                }
                            </Form.Control>
                        </Form.Group>
                        <Button variant="primary" type="submit">Submit</Button>
                    </Form>
                </Row>
            </>
        );
    }
}