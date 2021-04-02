import axios from 'axios';
import MsalHandler from '../components/auth/AuthService';

const ax = axios.create({
    baseURL: `https://localhost:5001/`,
});

const msalHandler = MsalHandler.getInstance();

ax.interceptors.request.use(
    async request => {
        console.debug("api::interceptor: request.url: " + request.url);
        var token = await msalHandler.acquireAccessToken(request.url);
        request.headers["Authorization"] = "Bearer " + token;
        return request;
    }
)

export default ax;