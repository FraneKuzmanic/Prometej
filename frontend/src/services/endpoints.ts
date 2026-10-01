import config from '../config.json';

const base = config.developmentBase;
export const baseUrl = base;

const userBase = `${base}/user`;
const periodBase = `${base}/period`;
const quizBase = `${base}/quiz`;

export const endpoints = {
  user: {
    base: userBase,
  },
  period: {
    base: periodBase,
  },
  quiz: {
    base: quizBase,
  },
};
