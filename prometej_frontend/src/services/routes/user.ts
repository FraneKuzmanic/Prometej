import axios from "axios";

import { endpoints } from "../endpoints";
import { LoginInput, UserCreateRequest } from "../../types/models/User";

const { user } = endpoints;

export default {
  register: (data: UserCreateRequest) => axios.post(`${user.base}/register`, data),
  login: (data: LoginInput) => axios.post(`${user.base}/login`, data),
  logout: () => axios.post(`${user.base}/logout`, null),
  getUser: (userId: string) => axios.get(`${user.base}/current-user/${userId}`,),
  deleteUser: (userid: number) => axios.delete(`${user.base}/${userid}`),
};