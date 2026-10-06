import axios from "axios";

import { endpoints } from "../endpoints";
import { TutorAskRequest, TutorDraftsRequest } from "../../types/models/Tutor";

const { tutor } = endpoints;

export default {
  status: () => axios.get(tutor.base),
  ask: (data: TutorAskRequest) => axios.post(`${tutor.base}/ask`, data),
  drafts: (data: TutorDraftsRequest) => axios.post(`${tutor.base}/drafts`, data),
};
