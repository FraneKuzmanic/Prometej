import { AnswerViewModel } from "../../types/models/Quiz";

// The Answers of a Quiz Game, Question by Question. They come in the order they were played,
// so the rows of one Question are next to each other.
export const groupAnswers = (answers: AnswerViewModel[]) => {
  const groups: AnswerViewModel[][] = [];
  answers.forEach((answer) => {
    const last = groups[groups.length - 1];
    if (last && last[0].questionId === answer.questionId) {
      last.push(answer);
    } else {
      groups.push([answer]);
    }
  });
  return groups;
};
