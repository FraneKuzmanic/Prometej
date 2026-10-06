const base = "/api";
export const baseUrl = base;

const userBase = `${base}/user`;
const periodBase = `${base}/period`;
const quizBase = `${base}/quiz`;
const discussionBase = `${base}/discussion`;
const sittingBase = `${base}/sitting`;

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
  discussion: {
    base: discussionBase,
  },
  sitting: {
    base: sittingBase,
  },
};
