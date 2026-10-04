export interface Period{
    id: number;
    name: string;
    timeFrame: string;
    description: string;
    // a file in the public folder; a Period without one gets a plain card
    image: string | null;
    // the Public Quizzes about this Period that have Questions
    quizCount: number;
}

export interface PeriodContentCreateRequest{
    periodId: number;
    content: string;
}

export interface PeriodContentEditRequest{
    id: number;
    periodId: string;
    content: string;
}

export interface PeriodContentViewModel{
    id: number;
    periodId: string;
    content: string;
}

export interface PeriodSearchPassage{
    // the nearest heading above the passage
    heading: string | null;
    text: string;
}

export interface PeriodSearchContent{
    periodId: number;
    periodName: string;
    // how many passages of the Period match; only the first few are sent
    matchCount: number;
    passages: PeriodSearchPassage[];
}