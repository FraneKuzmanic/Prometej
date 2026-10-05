import axios from "axios";

import { endpoints } from "../endpoints";
import { LoginInput, UserCreateRequest, UserNameEditRequest, UserPasswordEditRequest } from "../../types/models/User";
import ROLE from "../../types/enums/Role";

const { user } = endpoints;

export default {
  register: (data: UserCreateRequest) => axios.post(`${user.base}/register`, data),
  login: (data: LoginInput) => axios.post(`${user.base}/login`, data),
  logout: () => axios.post(`${user.base}/logout`, null),
  getUser: () => axios.get(`${user.base}/me`),
  deleteUser: () => axios.delete(`${user.base}/me`),
  updateName: (data: UserNameEditRequest) => axios.put(`${user.base}/me`, data),
  changePassword: (data: UserPasswordEditRequest) => axios.put(`${user.base}/me/password`, data),
  getUsers: () => axios.get(user.base),
  setRole: (id: number, role: ROLE) => axios.put(`${user.base}/${id}/role`, { role }),
};