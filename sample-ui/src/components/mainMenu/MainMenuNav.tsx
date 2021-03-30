import React from "react";
import Nav from "react-bootstrap/Nav";
import Navbar from "react-bootstrap/Navbar";
import Container from "react-bootstrap/Container";
import { Link, NavLink } from 'react-router-dom'
import AuthService from "../auth/AuthService";

interface Props {
  AuthService: AuthService;
  userName: string;
  authenticationStateChanged: any;
}

export default class MainMenuNav extends React.Component<Props> {
  auth: AuthService;
  constructor(p: Props) {
    super(p);
    this.auth = p.AuthService;
    this.state = { userName: p.userName };
  }

  login() {
    this.auth.login().then(x => {
      if (x) {
        this.props.authenticationStateChanged();
      }
    });
  }

  logout(e: any) {
    e.preventDefault();
    this.auth.logout();
  }


  render() {
    return (
      <Navbar bg="dark" variant="dark" expand="lg">
        <Container>
          <Navbar.Brand as={Link} to="/">msaljs</Navbar.Brand>
          <Navbar.Toggle aria-controls="basic-navbar-nav" />
          <Navbar.Collapse id="basic-navbar-nav">
            <Nav className="mr-auto">
              <Nav.Link as={NavLink} to="/" exact>Home</Nav.Link>
              <Nav.Link as={NavLink} to="/graph" exact>Graph</Nav.Link>
              <Nav.Link as={NavLink} to="/calendar" exact>Calendar</Nav.Link>
              <Nav.Link as={NavLink} to="/power" exact>Your API</Nav.Link>
              <Nav.Link as={NavLink} to="/groups" exact>Groups</Nav.Link>
              <Nav.Link as={NavLink} to="/approles" exact>AppRoles</Nav.Link>
              <Nav.Link as={NavLink} to="/claims" exact>Claims</Nav.Link>
            </Nav>
            <Navbar.Collapse className="justify-content-end">
              <Nav className="justify-content-end" style={{ width: "100%" }}>
                {(() => {
                  if (this.props.userName === "") {
                    return <button className="btn btn-primary" onClick={this.login.bind(this)}>Log in</button>
                  } else {
                    return <Navbar.Text>
                      <button className="btn btn-primary" onClick={this.logout.bind(this)}>Log out {this.props.userName}</button>
                    </Navbar.Text>
                  }
                })()}
              </Nav>
            </Navbar.Collapse>
          </Navbar.Collapse>
        </Container>
      </Navbar>
    );
  }
}