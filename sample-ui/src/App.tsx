import "./App.css";
import React, { Component } from "react";
import { BrowserRouter as Router, Switch, Route, Link } from "react-router-dom";
import Container from "react-bootstrap/Container";
import MainMenuNav from "./components/mainMenu/MainMenuNav";
import ClaimsView from "./components/claims/ClaimsView";
import Home from "./components/home/Home";

import AuthService from "./components/auth/AuthService";
import { OrganizationsView } from "./components/organizations/OrganizationsView";
import { GraphView } from "./components/organizations/GraphView";
import { PowerView } from "./components/power/PowerView";
import { Toast } from "react-bootstrap";
import { MsalHandler } from "./components/auth/AuthService";

interface State {
  userName: string;
  toastShow: boolean;
  toastMessage: string;
}

class App extends Component<any, State> {
  endpoint: string;
  msalHandler: MsalHandler;
  auth: AuthService;
  toastHandler: (s: boolean, m: string) => void;
  authenticationStateChanged: () => void;

  constructor(p: any, s: State) {
    super(p, s);

    this.endpoint = "https://msaljs.jpda.app/api";
    this.auth = this.msalHandler.getInstance();
    this.state = { userName: "", toastShow: false, toastMessage: "" };

    this.toastHandler = (s: boolean, m: string) => {
      this.setState({ toastShow: s, toastMessage: m });
      setTimeout(() => {
        this.setState({ toastShow: !s, toastMessage: m });
      }, 10000);
    };

    this.authenticationStateChanged = () => {
      if (this.auth.msalObj.getActiveAccount() !== null) {
        var account = this.auth.msalObj.getActiveAccount();
        this.setState({ userName: account === null ? "" : account.username });
      }
    };
  }

  render() {
    return (
      <Router>
        <div>
          <MainMenuNav AuthService={this.auth} userName={this.state.userName} key={this.state.userName} authenticationStateChanged={this.authenticationStateChanged} />
          <Container>
            <Route path="/" component={Home} />
            <Switch>
              <Route path="/organizations" render={(props) => <OrganizationsView {...props} auth={this.auth} toastToggle={this.toastHandler} authenticationStateChanged={this.authenticationStateChanged} />} />
              <Route path="/users" render={(props) => <CalendarView {...props} auth={this.auth} toastToggle={this.toastHandler} authenticationStateChanged={this.authenticationStateChanged} />} />
              <Route path="/apps" render={(props) => <PowerView {...props} endpoint={this.endpoint} auth={this.auth} toastToggle={this.toastHandler} authenticationStateChanged={this.authenticationStateChanged} />} />
              <Route path="/apps/roles" render={(props) => <ClaimsView {...props} auth={this.auth} toastToggle={this.toastHandler} />} />
              <Route path="/apps/roles/assign" render={(props) => <ClaimsView {...props} auth={this.auth} toastToggle={this.toastHandler} />} />
              <Route path="/claims" render={(props) => <ClaimsView {...props} auth={this.auth} toastToggle={this.toastHandler} />} />
            </Switch>
          </Container>
          <div style={{ position: 'absolute', top: 45, right: 15, minWidth: '24rem', zIndex: -1 }}>
            <Toast show={this.state.toastShow} onClose={() => { this.toastHandler(false, "") }}>
              <Toast.Header>
                <img src="//via.placeholder.com/20" className="rounded mr-2" alt="" />
                <strong className="mr-auto">Authentication error</strong>
                <small>Now</small>
              </Toast.Header>
              <Toast.Body>{this.state.toastMessage}</Toast.Body>
            </Toast>
          </div>
        </div>
      </Router >
    );
  }
}
export default App;