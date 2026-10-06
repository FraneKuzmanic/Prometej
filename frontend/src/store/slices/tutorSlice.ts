import { createAsyncThunk, createSlice } from "@reduxjs/toolkit";
import axios from "axios";

import tutorService from "../../services/routes/tutor";
import { TutorAnswer, TutorCitation, TutorKind, TutorTurn } from "../../types/models/Tutor";

export interface TutorMessage extends TutorTurn {
    kind?: TutorKind;
    citations?: TutorCitation[];
}

interface TutorState {
    // undefined until the server has said whether there is a tutor
    available: boolean | undefined;
    open: boolean;
    // The conversation lives here only: it survives a change of screen and not a reload.
    messages: TutorMessage[];
    pending: boolean;
    // the status of a failed question; "network" when no answer came at all
    error: number | "network" | undefined;
}

const initialState: TutorState = {
    available: undefined,
    open: false,
    messages: [],
    pending: false,
    error: undefined,
};

// What the server accepts with a question.
const MAX_TURNS = 20;
const MAX_TURN_LENGTH = 4000;

const statusOf = (error: unknown) => (axios.isAxiosError(error) ? error.response?.status : undefined);

const fetchTutorStatus = createAsyncThunk(
    'tutor/fetchStatus',
    async (): Promise<boolean> => {
        const response = await tutorService.status();
        return response.data.available;
    }
);

// retry: the question is already the last message and is sent once more.
const askTutor = createAsyncThunk<
    TutorAnswer,
    { question: string; periodId: number | null; retry?: boolean },
    { rejectValue: number | undefined; state: { tutor: TutorState } }
>(
    'tutor/ask',
    async ({ question, periodId }, { getState, rejectWithValue }) => {
        // The question itself is the last message by now.
        const history = getState().tutor.messages
            .slice(0, -1)
            .slice(-MAX_TURNS)
            .map(({ role, content }) => ({ role, content: content.slice(0, MAX_TURN_LENGTH) }));
        try {
            const response = await tutorService.ask({ question, history, periodId });
            return response.data;
        } catch (error) {
            return rejectWithValue(statusOf(error));
        }
    }
);

const tutorSlice = createSlice({
    name: 'tutor',
    initialState,
    reducers: {
        openTutor(state) {
            state.open = true;
        },
        closeTutor(state) {
            state.open = false;
        },
        newConversation(state) {
            state.messages = [];
            state.error = undefined;
        },
    },
    extraReducers: (builder) => {
        builder.addCase(fetchTutorStatus.fulfilled, (state, action) => {
            state.available = action.payload;
        });
        builder.addCase(fetchTutorStatus.rejected, (state) => {
            state.available = false;
        });
        builder.addCase(askTutor.pending, (state, action) => {
            state.pending = true;
            state.error = undefined;
            if (!action.meta.arg.retry) {
                state.messages.push({ role: "user", content: action.meta.arg.question });
            }
        });
        builder.addCase(askTutor.fulfilled, (state, action) => {
            state.pending = false;
            const { kind, answer, citations } = action.payload;
            state.messages.push({ role: "assistant", content: answer, kind, citations });
        });
        // The question stays on screen, so it can be sent again.
        builder.addCase(askTutor.rejected, (state, action) => {
            state.pending = false;
            state.error = action.payload ?? "network";
        });
    },
});

export { fetchTutorStatus, askTutor };
export const { openTutor, closeTutor, newConversation } = tutorSlice.actions;
export default tutorSlice.reducer;
