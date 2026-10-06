// "unverified": the tutor's quotes were not found in the material, so its answer is not shown.
export type TutorKind = "answer" | "not_covered" | "declined" | "unverified";

export interface TutorCitation{
    periodId: number;
    periodName: string;
    // the id the Period page gives the heading, so a citation is an address
    sectionId: string;
    sectionTitle: string;
    // found word for word in that section before the answer was sent
    quote: string;
}

export interface TutorAnswer{
    kind: TutorKind;
    answer: string;
    citations: TutorCitation[];
}

export interface TutorTurn{
    role: "user" | "assistant";
    content: string;
}

export interface TutorAskRequest{
    question: string;
    // the conversation so far; the server keeps none of it
    history: TutorTurn[];
    // the Period being read, if the question is asked on a Period's page
    periodId: number | null;
}
