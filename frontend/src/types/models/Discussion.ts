import ROLE from "../enums/Role";

// A row of a Period's Discussion.
export interface TopicSummary{
    id: number;
    title: string;
    // null when the author's account was deleted
    authorName: string | null;
    authorRole: ROLE | null;
    createdAt: string;
    replyCount: number;
    // the newest Reply's time, or the Topic's own
    lastActivityAt: string;
}

export interface TopicPage{
    topics: TopicSummary[];
    // every Topic of the Period, not only this page's
    total: number;
    pageSize: number;
}

export interface Reply{
    id: number;
    body: string;
    authorName: string | null;
    authorRole: ROLE | null;
    createdAt: string;
    // the server's answer for whoever asked; the client has no copy of the rule
    canDelete: boolean;
}

export interface Topic{
    id: number;
    periodId: number;
    title: string;
    body: string;
    authorName: string | null;
    authorRole: ROLE | null;
    createdAt: string;
    canDelete: boolean;
    replies: Reply[];
}

export interface TopicCreateRequest{
    title: string;
    body: string;
}

export interface ReplyCreateRequest{
    body: string;
}
