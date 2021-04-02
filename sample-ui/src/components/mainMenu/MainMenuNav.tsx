import React from "react";
import Nav from "react-bootstrap/Nav";
import Navbar from "react-bootstrap/Navbar";
import Container from "react-bootstrap/Container";
import { Link, NavLink } from 'react-router-dom'
import MsalHandler from "../auth/MsalHandler";

interface Props {
  auth: MsalHandler;
  userName: string;
}

export default class MainMenuNav extends React.Component<Props> {
  auth: MsalHandler;
  constructor(p: Props) {
    super(p);
    this.auth = p.auth;
    this.state = { userName: p.userName };
  }

  async login() {
    await this.auth.login();
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
              <Nav.Link as={NavLink} to="/users" exact>Users</Nav.Link>
              <Nav.Link as={NavLink} to="/apps" exact>Applications</Nav.Link>
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