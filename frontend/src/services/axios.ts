import axios from "axios"
import {store} from "../store/store";
import { clearUser, fetchCurrentUser } from "../store/slices/userSlice";
import { endpoints } from "./endpoints";

const { user } = endpoints;

const configureAxios = () => {
  axios.defaults.withCredentials = true;

  axios.interceptors.response.use(
    (response) => response,
    async (error) => {
      // The session check and the login form report their own 401 through their thunks.
      const url: string = error.config?.url ?? "";
      const isSessionCheck = url === `${user.base}/me` && error.config?.method === "get";
      const handledByThunk = isSessionCheck || url === `${user.base}/login`;

      // error.response is undefined when the request never reached the server.
      if (error.response?.status === 401 && !handledByThunk) {
        store.dispatch(clearUser());
      }
      // The server refused something this client offered: the Role in the store may be
      // older than the one in the database.
      if (error.response?.status === 403) {
        store.dispatch(fetchCurrentUser());
      }
      return Promise.reject(error);
    }
  );
};

export default configureAxios;
