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

export interface QuestionCreateRequest {
    questionTitle:string
    firstAnswer:string
    secondAnswer:string
    thirdAnswer:string
    fourthAnswer:string
    // which of the four answers is correct, 1 to 4; 0 while none is marked in the editor
    correctOption:number
    hintText:string
    exploreMore:string
}

export interface QuestionEditRequest {
    id: number;
    questionTitle:string
    firstAnswer:string
    secondAnswer:string
    thirdAnswer:string
    fourthAnswer:string
    correctOption:number
    hintText:string
    exploreMore:string
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
}

export interface QuestionViewModel {
    id: number;
    questionTitle:string
    firstAnswer:string
    secondAnswer:string
    thirdAnswer:string
    fourthAnswer:string
    correctOption:number
    hintText:string | null
    exploreMore:string | null
}

export interface AnswerCreateRequest{
    questionId:number;
    chosenOption:number;
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
    isRetired: boolean;
    answerCount: number;
    correctCount: number;
    // the wrong answer chosen most often, as its text was when played; null if nobody was wrong
    mostChosenWrongAnswer: string | null;
    mostChosenWrongCount: number;
}

export interface PlayerSummary {
    userId: number;
    userName: string;
    gameCount: number;
    // a Score goes with the number of Questions its game was played with
    firstScore: number;
    firstQuestionCount: number;
    // the game with the highest share of correct answers
    bestScore: number;
    bestQuestionCount: number;
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
}

export interface PlayedQuizGame {
    id: number;
    quizId: number;
    quizTitle: string;
    periodName: string | null;
    score: number;
    // how many Questions the game was played with, not how many the Quiz has now
    questionCount: number;
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