import axios from "axios"
import {store} from "../store/store";
import { clearUser } from "../store/slices/userSlice";


const configureAxios = () => {
  axios.defaults.withCredentials = true;

  axios.interceptors.response.use(
    (response) => response,
    async (error) => {
      if (error.response.status === 401) {
        store.dispatch(clearUser());
      }
      return Promise.reject(error);
    }
  );
};

export default configureAxios;