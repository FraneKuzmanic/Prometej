import axios from "axios";

import { endpoints } from "../endpoints";
import { PeriodContentEditRequest } from "../../types/models/Period";

const { period } = endpoints;

export default {
  edit: (data: PeriodContentEditRequest) => axios.post(`${period.base}/content`, data),
  get: (id: string) => axios.get(`${period.base}/content/${id}`,),
  search: (query: string) => axios.get(`${period.base}/content/search/${query}`),
};