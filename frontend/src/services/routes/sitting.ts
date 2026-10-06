import axios from "axios";

import { endpoints } from "../endpoints";
import { SittingAnswerRequest } from "../../types/models/Sitting";

const { sitting } = endpoints;

export default {
  // code is the Entry Code of a Test; a Public Quiz is sat without one
  info: (quizId: number, code?: string) => axios.get(`${sitting.base}/info/${quizId}`, { params: { code } }),
  start: (quizId: number, code?: string) => axios.post(`${sitting.base}/start/${quizId}`, null, { params: { code } }),
  saveAnswer: (sittingId: number, data: SittingAnswerRequest) => axios.put(`${sitting.base}/${sittingId}/answer`, data),
  finish: (sittingId: number) => axios.post(`${sitting.base}/${sittingId}/finish`),
  // gives up a running Sitting of a Public Quiz
  discard: (sittingId: number) => axios.delete(`${sitting.base}/${sittingId}`),
  // for the Quiz's Creator: lets the Student sit the Test again
  reset: (sittingId: number) => axios.post(`${sitting.base}/${sittingId}/reset`),
};
