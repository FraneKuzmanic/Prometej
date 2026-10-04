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