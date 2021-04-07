import "./App.css";
import 'bootstrap/dist/css/bootstrap.min.css';
import React, { Component } from "react";
import { BrowserRouter as Router, Switch, Route, RouteComponentProps } from "react-router-dom";
import Container from "react-bootstrap/Container";
import MainMenuNav from "./components/mainMenu/MainMenuNav";
import ClaimsView from "./components/claims/ClaimsView";

import { UsersView } from "./components/users/UsersView";
import MsalHandler from "./components/auth/MsalHandler";
import { APIClient } from "./api/APIClient";
import { AppsView } from "./components/apps/AppsView";
import { AppAssignmentsView } from "./components/apps/AppAssignmentsView";
import { AppAssignmentForm } from "./components/apps/AppAssignmentForm";

interface State {
  userName: string;
  toastShow: boolean;
  toastMessage: string;
}

class App extends Component<any, State> {
  msalHandler: MsalHandler = MsalHandler.getInstance();
  endpoint: string = "https://localhost:5001/";
  toastHandler: (s: boolean, m: string) => void;
  authenticationStateChanged: () => void;
  apiClient: APIClient = new APIClient(this.endpoint);

  constructor(p: any, s: State) {
    super(p, s);
    this.state = { userName: "", toastShow: false, toastMessage: "" };

    this.toastHandler = (s: boolean, m: string) => {
      this.setState({ toastShow: s, toastMessage: m });
      setTimeout(() => {
        this.setState({ toastShow: !s, toastMessage: m });
      }, 10000);
    };

    this.authenticationStateChanged = () => {
      console.log("authstate changed");
      if (this.msalHandler.msalObj.getActiveAccount() !== null) {
        var account = this.msalHandler.msalObj.getActiveAccount();
        this.setState({ userName: account === null ? "" : account.username });
      }
    };
    this.msalHandler.setCallback(this.authenticationStateChanged);
  }

  render() {
    return (
      <Router>
        <div>
          <MainMenuNav auth={this.msalHandler} userName={this.state.userName} key={this.state.userName} />
          <Container>
            {/* <Route path="/" component={Home} /> */}
            <Switch>
              <Route path="/users"><UsersView apiClient={this.apiClient} /></Route>
              <Route path="/apps" exact><AppsView apiClient={this.apiClient} /></Route>
              <Route path="/apps/:resourceId/assignments" exact render={({ match }: MatchProps) => (<AppAssignmentsView apiClient={this.apiClient} resourceId={match.params.resourceId} />)} />
              <Route path="/apps/:resourceId/assignments/add" render={({ match }: MatchProps) => (<AppAssignmentForm apiClient={this.apiClient} resourceId={match.params.resourceId} />)} />
              <Route path="/claims"><ClaimsView auth={this.msalHandler}/></Route>
            </Switch>
          </Container>
        </div>
      </Router >
    );
  }
}

interface MatchParams {
  resourceId: string;
}

interface MatchProps extends RouteComponentProps<MatchParams> {
}
export default App;