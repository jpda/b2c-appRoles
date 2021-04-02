import React from "react";
import Card from "react-bootstrap/Card";
import { LinkContainer } from 'react-router-bootstrap';
import Row from "react-bootstrap/Row";
import {  Button, Collapse, CardGroup } from "react-bootstrap";

interface State {
    open: boolean;
}

export default class Home extends React.Component<State> {
    state: State;

    constructor(state: State) {
        super(state);
        this.state = { open: true };
    }

    setCollapsedState() {
        this.setState({ open: !this.state.open });
    }

    render() {
        return (
            <>
                <Row>
                    <Button onClick={() => this.setCollapsedState()}
                        aria-controls="example-collapse-text"
                        aria-expanded={this.state.open}>{this.state.open ? "Hide options" : "Show options"}</Button>
                </Row>
                <Collapse in={this.state.open}>
                    <Row>
                        <CardGroup>
                            <Card>
                                <Card.Body>
                                    <Card.Title>Microsoft Graph</Card.Title>
                                    <Card.Subtitle className="mb-2 text-muted">For viewing user data</Card.Subtitle>
                                    <Card.Text>
                                        View basic user data from the Graph. Uses the Graph <code>User.Read</code> scope,
                                        which is pre-configured in the app registration, called <a href="https://docs.microsoft.com/en-us/azure/active-directory/develop/v1-permissions-and-consent#types-of-consent" target="_blank" rel="noopener noreferrer">Static</a> consent. 
                                        The <code>Calendar.Read</code> scope is incrementally added.
                                    </Card.Text>
                                </Card.Body>
                                <Card.Body>
                                    <LinkContainer to="/graph">
                                        <Card.Link>See my basic Graph user object</Card.Link>
                                    </LinkContainer>
                                    <LinkContainer to="/calendar">
                                        <Card.Link>Incrementally check my calendar</Card.Link>
                                    </LinkContainer>
                                </Card.Body>
                                <Card.Footer>
                                    <small className="text-muted">Served by Microsoft Graph</small>
                                </Card.Footer>
                            </Card>
                            <Card>
                                <Card.Body>
                                    <Card.Title>Your API (Dynamic)</Card.Title>
                                    <Card.Subtitle className="mb-2 text-muted">For accessing your own API</Card.Subtitle>
                                    <Card.Text>
                                        Fetches data from your own Azure AD-protected API. Requests consent to scopes at runtime, called <a href="https://docs.microsoft.com/en-us/azure/active-directory/develop/azure-ad-endpoint-comparison#incremental-and-dynamic-consent" target="_blank" rel="noopener noreferrer">dynamic</a> consent.
                                    </Card.Text>
                                </Card.Body>
                                <Card.Body>
                                    <LinkContainer to="/power">
                                        <Card.Link>Call your API</Card.Link>
                                    </LinkContainer>
                                </Card.Body>
                                <Card.Footer>
                                    <small className="text-muted">Served by Your API</small>
                                </Card.Footer>
                            </Card>
                            <Card>
                                <Card.Body>
                                    <Card.Title>Group claim data</Card.Title>
                                    <Card.Subtitle className="mb-2 text-muted">Include group data in claims</Card.Subtitle>
                                    <Card.Text>
                                        Shows group data included in claims in a received <code>id_token</code> or <code>access_token</code> with the <code>groupMembershipClaims</code> attribute.
                    </Card.Text>
                                </Card.Body>
                                <Card.Body>
                                    <LinkContainer to="/groups">
                                        <Card.Link>See my groups</Card.Link>
                                    </LinkContainer>
                                </Card.Body>
                                <Card.Footer>
                                    <small className="text-muted">Served by Azure AD</small>
                                </Card.Footer>
                            </Card>
                            <Card>
                                <Card.Body>
                                    <Card.Title>AppRole claim data</Card.Title>
                                    <Card.Subtitle className="mb-2 text-muted">View user's role data</Card.Subtitle>
                                    <Card.Text>
                                        Shows AppRole data included in claims for making authorization decisions. 
                    </Card.Text>
                                </Card.Body>
                                <Card.Body>
                                    <LinkContainer to="/approles">
                                        <Card.Link>See my AppRoles</Card.Link>
                                    </LinkContainer>
                                </Card.Body>
                                <Card.Footer>
                                    <small className="text-muted">Served by Azure AD</small>
                                </Card.Footer>
                            </Card>
                            <Card>
                                <Card.Body>
                                    <Card.Title>Claims</Card.Title>
                                    <Card.Subtitle className="mb-2 text-muted">View claims</Card.Subtitle>
                                    <Card.Text>View authentication claims that got you here</Card.Text>
                                </Card.Body>
                                <Card.Body>
                                    <LinkContainer to="/claims">
                                        <Card.Link>See my claims</Card.Link>
                                    </LinkContainer>
                                </Card.Body>
                                <Card.Footer>
                                    <small className="text-muted">Served by Azure AD</small>
                                </Card.Footer>
                            </Card>
                        </CardGroup>
                    </Row >
                </Collapse>
            </>
        );
    }
}