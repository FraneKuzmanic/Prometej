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

export interface PeriodSearchContent{
    periodId: number;
    searchContent: string;
}