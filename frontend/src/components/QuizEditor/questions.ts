import {
  QuestionCreateRequest,
  QuestionEditRequest,
  QuizViewModel,
  SourceTextRequest,
} from "../../types/models/Quiz";

// A Question already stored has an id; one added in the editor has none yet. A Question asked
// about a Source Text names it by the key the editor holds that text under.
export type EditorQuestion = Omit<QuestionCreateRequest, "sourceTextNo"> & {
  id?: number;
  passageKey?: string;
};

export type EditorPassages = Record<string, SourceTextRequest>;

export const emptyQuestion: EditorQuestion = {
  questionTitle: "",
  firstAnswer: "",
  secondAnswer: "",
  thirdAnswer: "",
  fourthAnswer: "",
  correctOption: 0,
  hintText: "",
  exploreMore: "",
};

// The server's rule for a Question, checked here first so the Teacher is told which one.
export const isComplete = (question: EditorQuestion) => {
  const options = [
    question.firstAnswer,
    question.secondAnswer,
    question.thirdAnswer,
    question.fourthAnswer,
  ].map((option) => option.trim());
  return (
    question.questionTitle.trim() !== "" &&
    options.every((option) => option !== "") &&
    new Set(options).size === options.length &&
    question.correctOption >= 1 &&
    question.correctOption <= 4
  );
};

export const isPassageComplete = (passage: SourceTextRequest) =>
  passage.caption.trim() !== "" && passage.body.trim() !== "";

// What the server takes: the Source Texts in the order the Questions first use them, and
// each Question naming its text by its number in that list.
export const toRequest = (
  questions: EditorQuestion[],
  passages: EditorPassages
): { questions: QuestionEditRequest[]; sourceTexts: SourceTextRequest[] } => {
  const keys: string[] = [];
  const sent = questions.map(({ passageKey, id, ...question }) => {
    if (passageKey && !keys.includes(passageKey)) keys.push(passageKey);
    return {
      ...question,
      id: id ?? 0,
      sourceTextNo: passageKey ? keys.indexOf(passageKey) + 1 : undefined,
    };
  });
  return { questions: sent, sourceTexts: keys.map((key) => passages[key]) };
};

// A stored Quiz as the editor holds it.
export const fromQuiz = (
  quiz: QuizViewModel
): { questions: EditorQuestion[]; passages: EditorPassages } => {
  const passages: EditorPassages = {};
  quiz.sourceTexts.forEach((sourceText) => {
    passages[`stored-${sourceText.id}`] = { ...sourceText };
  });
  const questions = quiz.questions.map(({ sourceTextId, ...question }) => ({
    ...question,
    hintText: question.hintText ?? "",
    exploreMore: question.exploreMore ?? "",
    passageKey: sourceTextId === null ? undefined : `stored-${sourceTextId}`,
  }));
  return { questions, passages };
};
