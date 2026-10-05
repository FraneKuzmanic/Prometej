import axios from "axios";

import { endpoints } from "../endpoints";
import { ReplyCreateRequest, TopicCreateRequest } from "../../types/models/Discussion";

const { discussion } = endpoints;

export default {
  getTopics: (periodId: number, page: number) => axios.get(`${discussion.base}/period/${periodId}`, { params: { page } }),
  getTopic: (id: number) => axios.get(`${discussion.base}/topic/${id}`),
  createTopic: (periodId: number, data: TopicCreateRequest) => axios.post(`${discussion.base}/period/${periodId}`, data),
  createReply: (topicId: number, data: ReplyCreateRequest) => axios.post(`${discussion.base}/topic/${topicId}/reply`, data),
  deleteTopic: (id: number) => axios.delete(`${discussion.base}/topic/${id}`),
  deleteReply: (id: number) => axios.delete(`${discussion.base}/reply/${id}`),
};
