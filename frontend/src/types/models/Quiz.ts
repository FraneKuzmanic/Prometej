export interface QuizBaseModel {
    id: number;
    title: string;
    creatorName: string;
    isPrivate: boolean;
    entryCode?: number;
}

export interface QuestionCreateRequest {
    questionTitle:string
    firstAnswer:string
    secondAnswer:string
    thirdAnswer:string
    fourthAnswer:string
    correctAnswer:string
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
    correctAnswer:string
    hintText:string
    exploreMore:string
}

export interface QuizCreateRequest {
    title: string;
    isPrivate: boolean;
    creatorId: number;
    entryCode: number;
}

export interface QuizEditRequest {
    id: number;
    title: string;
    creatorId: number;
    isPrivate: boolean;
    entryCode?: number;
}

export interface QuizViewModel {
    id: number;
    title: string;
    isPrivate: boolean;
    creatorId: number;
    creatorName: string;
    entryCode?: number;
    questions: QuestionViewModel[];
}

export interface QuestionViewModel {
    id: number;
    questionTitle:string
    firstAnswer:string
    secondAnswer:string
    thirdAnswer:string
    fourthAnswer:string
    correctAnswer:string
    hintText:string
    exploreMore:string
}

export interface AnswerCreateRequest{
    questionId:number;
    answerText:string;
    correctAnswer:string;
}

export interface AnswerViewModel {
    id: number
    quizGameId: number;
    questionId:number;
    answerText:string;
    correctAnswer:string;
}

export interface QuizGameCreateRequest {
    quizId: number;
    userId?: number;
    userName?: string;
    score: number;
    datePlayed: Date;
}

export interface QuizGameViewModel {
    id: number;
    quizId: number;
    userId?: number;
    userName: string;
    score: number;
    datePlayed: Date;
    answers: AnswerViewModel[];
}