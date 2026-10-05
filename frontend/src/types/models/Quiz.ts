export interface QuizBaseModel {
    id: number;
    title: string;
    creatorName: string;
    isPrivate: boolean;
    entryCode?: number;
    // the Period the Quiz is about, if it says one
    periodId: number | null;
    periodName: string | null;
    questionCount: number;
    // only in a Creator's own list: how many Quiz Games deleting the Quiz would delete
    quizGameCount?: number;
}

// "choice": four options, one correct. "matching": pairs to connect. "ordering": items to
// put in order. The last two give a point for each right pair or place.
export type QuestionType = "choice" | "matching" | "ordering";

export interface MatchPair {
    left: string;
    right: string;
}

// What a matching or an ordering Question asks. A play names these by number, from 1, in
// the order they are stored in: the pairs' right-hand texts first, then the extras.
export interface QuestionContent {
    pairs?: MatchPair[];
    // right-hand options that go with nothing
    extras?: string[];
    // in their right order
    items?: string[];
}

export interface QuestionCreateRequest {
    questionTitle:string
    type:QuestionType
    // the four answers and the correct one, 1 to 4: a choice Question only
    firstAnswer?:string
    secondAnswer?:string
    thirdAnswer?:string
    fourthAnswer?:string
    correctOption?:number
    // a matching or an ordering Question only
    content?:QuestionContent
    hintText:string
    exploreMore:string
    // the 1-based number of the Question's Source Text in the request's list, if it has one
    sourceTextNo?:number
}

export interface QuestionEditRequest extends QuestionCreateRequest {
    // 0 for a Question added in this edit
    id: number;
}

// A passage some of a Quiz's Questions are asked about. id is 0 until it is stored.
export interface SourceTextRequest {
    id: number;
    caption: string;
    body: string;
}

export interface SourceTextViewModel {
    id: number;
    caption: string;
    // plain text; its line breaks are part of it
    body: string;
}

export interface QuizCreateRequest {
    title: string;
    isPrivate: boolean;
    periodId: number | null;
}

export interface QuizEditRequest {
    id: number;
    title: string;
    isPrivate: boolean;
    // Sent with every update: the server reads a request without it as "no Period".
    periodId: number | null;
}

export interface QuizViewModel {
    id: number;
    title: string;
    isPrivate: boolean;
    creatorId: number;
    creatorName: string;
    entryCode?: number;
    periodId: number | null;
    periodName: string | null;
    questions: QuestionViewModel[];
    // the Source Texts those Questions are asked about
    sourceTexts: SourceTextViewModel[];
}

export interface QuestionViewModel {
    id: number;
    questionTitle:string
    type:QuestionType
    // null unless the Question is a choice Question
    firstAnswer:string | null
    secondAnswer:string | null
    thirdAnswer:string | null
    fourthAnswer:string | null
    correctOption:number | null
    // null for a choice Question
    content:QuestionContent | null
    hintText:string | null
    exploreMore:string | null
    sourceTextId:number | null
}

// One Question's answer as numbers; which of the three depends on the Question's type.
export interface AnswerCreateRequest{
    questionId:number;
    // choice: the option chosen, 1 to 4
    chosenOption?:number;
    // matching: for each pair in order, the number of the right-hand option chosen
    matches?:number[];
    // ordering: for each place in order, the number of the item put there
    order?:number[];
}

export interface AnswerViewModel {
    id: number
    quizGameId: number;
    questionId:number;
    // the Question's title and Explore More and the two answers, as they were when played
    questionTitle:string;
    exploreMore:string | null;
    answerText:string;
    correctAnswer:string;
    // A Question of several points has a row for each: a matching Question's row names its
    // left-hand item, an ordering Question's its place. A choice Question's row has neither.
    item:string | null;
    place:number | null;
    // its ordinal in the Quiz Game
    position:number;
    // the Source Text the Question was played beside, in the version shown then
    sourceTextId:number | null;
}

export interface QuizGameViewModel {
    id: number;
    quizId: number;
    userId: number;
    userName: string;
    score: number;
    datePlayed: string;
    answers: AnswerViewModel[];
}

export interface QuestionReport {
    questionId: number;
    // the title as it is today; the counts are over the Answers as they were played
    questionTitle: string;
    type: QuestionType;
    isRetired: boolean;
    // the Question's Source Text as it is today, and its caption; null without one
    sourceTextId: number | null;
    sourceTextCaption: string | null;
    // points possible and points won, over every play
    answerCount: number;
    correctCount: number;
    // the wrong answer chosen most often, as its text was when played; null if nobody was
    // wrong, and for a Question of several points, whose lines say it for each
    mostChosenWrongAnswer: string | null;
    mostChosenWrongCount: number;
    // one for each left-hand item of a matching Question or place of an ordering one
    lines: QuestionLine[];
}

export interface QuestionLine {
    // the left-hand item, or the number of the place
    label: string;
    answerCount: number;
    correctCount: number;
    mostChosenWrongAnswer: string | null;
    mostChosenWrongCount: number;
}

export interface PlayerSummary {
    userId: number;
    userName: string;
    gameCount: number;
    // a Score goes with the points its game could give
    firstScore: number;
    firstMaxScore: number;
    // the game with the highest share of points won
    bestScore: number;
    bestMaxScore: number;
    lastPlayed: string;
}

// How a Quiz was played, for its Creator: every Quiz Game, newest first, and the same
// games counted by Question and by player.
export interface QuizAnalytics {
    quizTitle: string;
    games: QuizGameViewModel[];
    questions: QuestionReport[];
    players: PlayerSummary[];
}

// A Quiz Game as its player reads it again, with its Quiz as it is today.
export interface QuizGameReviewViewModel extends QuizGameViewModel {
    quizTitle: string;
    periodId: number | null;
    periodName: string | null;
    // whether the public list still shows the Quiz, so it can be played again
    quizIsListed: boolean;
    // the Source Texts the Answers were given beside, as they were then
    sourceTexts: SourceTextViewModel[];
}

export interface PlayedQuizGame {
    id: number;
    quizId: number;
    quizTitle: string;
    periodName: string | null;
    score: number;
    // the points the game could give when it was played
    maxScore: number;
    datePlayed: string;
}

export interface PeriodProgress {
    periodId: number;
    periodName: string;
    // how many Quizzes the public list shows for the Period
    quizCount: number;
    // how many of those the player has played
    playedCount: number;
    // the player's best play of each played Quiz, averaged
    averageBestPercent: number;
}

export interface MyQuizGames {
    progress: PeriodProgress[];
    games: PlayedQuizGame[];
}