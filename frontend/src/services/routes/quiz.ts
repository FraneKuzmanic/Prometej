import axios from "axios";

import { endpoints } from "../endpoints";
import { CreateQuizPayload, SubmitQuizPayload, UpdateQuizPayload } from "../../store/slices/quizSlice";


const { quiz } = endpoints;

export default {
  getAllUserQuizzes: (userId: number) => axios.get(`${quiz.base}/getAllUserQuizzes/${userId}`),
  search: (query: string, periodId?: number) => axios.get(`${quiz.base}/search`, { params: { query, periodId } }),
  get: (quizId: number, code?: string) => axios.get(`${quiz.base}/get/${quizId}`, { params: { code } }),
  create: (data: CreateQuizPayload) => axios.post(`${quiz.base}/create`, data),
  Update: (data: UpdateQuizPayload) => axios.put(`${quiz.base}/update`, data),
  delete: (quizId: number) => axios.delete(`${quiz.base}/delete/${quizId}`),
  submit: (data: SubmitQuizPayload) => axios.post(`${quiz.base}/submit`, data),
  getQuizAnalytics: (quizId: number) => axios.get(`${quiz.base}/getAnalytics/${quizId}`),
  getByCode: (quizCode: string) => axios.get(`${quiz.base}/getByCode/${quizCode}`),
};