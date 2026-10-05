import {
  MatchPair,
  QuestionEditRequest,
  QuestionType,
  QuizViewModel,
  SourceTextRequest,
} from "../../types/models/Quiz";

// A Question as the editor holds it, with the fields of every type; only those of its own
// type are shown and sent. One already stored has an id. A Question asked about a Source Text
// names it by the key the editor holds that text under.
export interface EditorQuestion {
  id?: number;
  type: QuestionType;
  questionTitle: string;
  firstAnswer: string;
  secondAnswer: string;
  thirdAnswer: string;
  fourthAnswer: string;
  // which of the four answers is correct, 1 to 4; 0 while none is marked
  correctOption: number;
  pairs: MatchPair[];
  // right-hand options that go with nothing; an empty one is not sent
  extras: string[];
  // an ordering Question's items, in their right order
  items: string[];
  hintText: string;
  exploreMore: string;
  passageKey?: string;
}

export type EditorPassages = Record<string, SourceTextRequest>;

// The server's limits for a matching and for an ordering Question.
export const MIN_PAIRS = 3;
export const MAX_PAIRS = 5;
export const MAX_EXTRAS = 2;
export const MIN_ITEMS = 3;
export const MAX_ITEMS = 6;

export const newQuestion = (type: QuestionType): EditorQuestion => ({
  type,
  questionTitle: "",
  firstAnswer: "",
  secondAnswer: "",
  thirdAnswer: "",
  fourthAnswer: "",
  correctOption: 0,
  pairs:
    type === "matching"
      ? Array.from({ length: MIN_PAIRS }, () => ({ left: "", right: "" }))
      : [],
  extras: [],
  items: type === "ordering" ? Array.from({ length: MIN_ITEMS }, () => "") : [],
  hintText: "",
  exploreMore: "",
});

const allDiffer = (texts: string[]) =>
  texts.every((text) => text !== "") && new Set(texts).size === texts.length;

const filledExtras = (question: EditorQuestion) =>
  question.extras.map((extra) => extra.trim()).filter((extra) => extra !== "");

// The server's rule for a Question, checked here first so the Teacher is told which one.
export const isComplete = (question: EditorQuestion) => {
  if (question.questionTitle.trim() === "") return false;
  if (question.type === "matching") {
    const lefts = question.pairs.map((pair) => pair.left.trim());
    const rights = question.pairs.map((pair) => pair.right.trim());
    return (
      question.pairs.length >= MIN_PAIRS &&
      question.pairs.length <= MAX_PAIRS &&
      allDiffer(lefts) &&
      allDiffer([...rights, ...filledExtras(question)])
    );
  }
  if (question.type === "ordering") {
    return (
      question.items.length >= MIN_ITEMS &&
      question.items.length <= MAX_ITEMS &&
      allDiffer(question.items.map((item) => item.trim()))
    );
  }
  const options = [
    question.firstAnswer,
    question.secondAnswer,
    question.thirdAnswer,
    question.fourthAnswer,
  ].map((option) => option.trim());
  return (
    allDiffer(options) &&
    question.correctOption >= 1 &&
    question.correctOption <= 4
  );
};

export const isPassageComplete = (passage: SourceTextRequest) =>
  passage.caption.trim() !== "" && passage.body.trim() !== "";

// What the server takes: each Question with the fields of its type and no others, the
// Source Texts in the order the Questions first use them, and each Question naming its text
// by its number in that list.
export const toRequest = (
  questions: EditorQuestion[],
  passages: EditorPassages
): { questions: QuestionEditRequest[]; sourceTexts: SourceTextRequest[] } => {
  const keys: string[] = [];
  const sent = questions.map((question): QuestionEditRequest => {
    const { passageKey } = question;
    if (passageKey && !keys.includes(passageKey)) keys.push(passageKey);
    const shared = {
      id: question.id ?? 0,
      type: question.type,
      questionTitle: question.questionTitle,
      hintText: question.hintText,
      exploreMore: question.exploreMore,
    };
    if (question.type === "matching") {
      const extras = filledExtras(question);
      return {
        ...shared,
        content: { pairs: question.pairs, ...(extras.length > 0 && { extras }) },
      };
    }
    if (question.type === "ordering") {
      return { ...shared, content: { items: question.items } };
    }
    return {
      ...shared,
      firstAnswer: question.firstAnswer,
      secondAnswer: question.secondAnswer,
      thirdAnswer: question.thirdAnswer,
      fourthAnswer: question.fourthAnswer,
      correctOption: question.correctOption,
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
  const questions = quiz.questions.map(
    (question): EditorQuestion => ({
      id: question.id,
      type: question.type,
      questionTitle: question.questionTitle,
      firstAnswer: question.firstAnswer ?? "",
      secondAnswer: question.secondAnswer ?? "",
      thirdAnswer: question.thirdAnswer ?? "",
      fourthAnswer: question.fourthAnswer ?? "",
      correctOption: question.correctOption ?? 0,
      pairs: question.content?.pairs ?? [],
      extras: question.content?.extras ?? [],
      items: question.content?.items ?? [],
      hintText: question.hintText ?? "",
      exploreMore: question.exploreMore ?? "",
      passageKey:
        question.sourceTextId === null
          ? undefined
          : `stored-${question.sourceTextId}`,
    })
  );
  return { questions, passages };
};
