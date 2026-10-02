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
    // how many passages of the Period match; only the first few are sent
    matchCount: number;
    passages: PeriodSearchPassage[];
}