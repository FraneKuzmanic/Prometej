import axios from "axios"
import {store} from "../store/store";
import { clearUser } from "../store/slices/userSlice";


const configureAxios = () => {
  axios.defaults.withCredentials = true;

  axios.interceptors.response.use(
    (response) => response,
    async (error) => {
      // The session check and the login form report their own 401 through their thunks.
      const url: string = error.config?.url ?? "";
      const handledByThunk = url.endsWith("/user/me") || url.endsWith("/user/login");

      // error.response is undefined when the request never reached the server.
      if (error.response?.status === 401 && !handledByThunk) {
        store.dispatch(clearUser());
      }
      return Promise.reject(error);
    }
  );
};

export default configureAxios;
