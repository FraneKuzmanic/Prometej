import { QuestionType, SourceTextViewModel } from "./Quiz";

// What the start screen shows of a Quiz that can be sat, and where the caller stands with it.
export interface SittingInfo {
    quizId: number;
    title: string;
    isTest: boolean;
    questionCount: number;
    maxScore: number;
    timeLimitMinutes: number | null;
    closesAt: string | null;
    isClosed: boolean;
    // the caller made the Quiz, and so cannot sit it
    isOwn: boolean;
    // the caller has a Sitting of it running
    running: boolean;
    // the caller's Sitting of a Test, once it has ended
    result: SittingResult | null;
}

// How a Sitting ended. The Answers are the review's, never part of this.
export interface SittingResult {
    gameId: number;
    score: number;
    maxScore: number;
    // "expired": the time limit or the Test's closing ended it
    outcome: "submitted" | "expired";
    // false for a Test that is not closed yet
    reviewAvailable: boolean;
}

// A Question as a Sitting shows it: no answer is in it. The lists of a matching and an
// ordering Question come shuffled by the server, and an answer names them by number, from 1,
// in the order they came in.
export interface SittingQuestion {
    questionId: number;
    questionTitle: string;
    type: QuestionType;
    // choice: the four answers
    options: string[] | null;
    // matching: the left-hand items, and the right-hand options to link them to
    lefts: string[] | null;
    rights: string[] | null;
    // ordering: the items to put in order
    items: string[] | null;
    sourceTextId: number | null;
    // what was saved for it so far; null until it is first answered
    given: number[] | null;
}

export interface Sitting {
    id: number;
    quizId: number;
    quizTitle: string;
    isTest: boolean;
    startedAt: string;
    // when the server ends it; null without a time limit or a closing time
    endsAt: string | null;
    // the server's clock when this was sent
    serverNow: string;
    questions: SittingQuestion[];
    sourceTexts: SourceTextViewModel[];
}

// One Question's answer: a number for each of its points, 0 for one left empty.
export interface SittingAnswerRequest {
    questionId: number;
    given: number[];
}
