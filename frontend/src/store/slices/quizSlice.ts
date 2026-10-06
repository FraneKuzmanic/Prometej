import { PayloadAction, createAsyncThunk, createSlice } from "@reduxjs/toolkit";
import axios from "axios";

import QuizService from "../../services/routes/quiz";
import { AnswerCreateRequest, QuestionCreateRequest, QuestionEditRequest, MyQuizGames, QuizAnalytics, QuizBaseModel, QuizCreateRequest, QuizEditRequest, QuizGameReviewViewModel, QuizGameViewModel, QuizViewModel, SourceTextRequest } from "../../types/models/Quiz";

interface QuizState {
    quizzes: QuizBaseModel[] | undefined;
    quiz: QuizViewModel | undefined;
    analytics: QuizAnalytics | undefined;
    analyticsFailed: boolean;
    // the signed-in User's own Quiz Games and their progress per Period
    myGames: MyQuizGames | undefined;
    myGamesFailed: boolean;
    // one of those Quiz Games, opened for review
    gameReview: QuizGameReviewViewModel | undefined;
    gameReviewFailed: boolean;
    // the Quiz Game is a Test's, and the Test is not closed yet
    gameReviewLocked: boolean;
    // the Quiz Game the server stored for the play just finished
    lastGame: QuizGameViewModel | undefined;
    // "rejected": the server refused the submission, so sending it again cannot help.
    // "failed": the request did not get an answer, or got a server error.
    submitStatus: "idle" | "pending" | "saved" | "rejected" | "failed";
}

export interface CreateQuizPayload {
    quiz: QuizCreateRequest;
    questions: QuestionCreateRequest[];
    sourceTexts?: SourceTextRequest[];

}
export interface UpdateQuizPayload {
    quiz: QuizEditRequest;
    questions?: QuestionEditRequest[];
    // read by the server only together with the questions
    sourceTexts?: SourceTextRequest[];

}

export interface SubmitQuizPayload {
    quizId: number;
    answers: AnswerCreateRequest[];
    // One per play: the server stores a play once, however often its submit is sent.
    submissionKey: string;
}

const initialState: QuizState = {
    quizzes: undefined,
    quiz: undefined,
    analytics: undefined,
    analyticsFailed: false,
    myGames: undefined,
    myGamesFailed: false,
    gameReview: undefined,
    gameReviewFailed: false,
    gameReviewLocked: false,
    lastGame: undefined,
    submitStatus: "idle",
};

const fetchMyQuizzes = createAsyncThunk(
    'quiz/getMyQuizzes',
    async () => {
        const response = await QuizService.getMyQuizzes();
        return response.data;
    }
);

const searchQuizzes = createAsyncThunk(
    'quiz/search',
    // The public list: all of it, or the Quizzes of one Period, or those a query finds.
    async ({ query, periodId }: { query: string; periodId?: number }) => {
        const response = await QuizService.search(query, periodId);
        return response.data;
    }
);

const fetchQuiz = createAsyncThunk(
    'quiz/get',
    // code is the Entry Code, needed to open a Private Quiz the caller did not create
    async ({ quizId, code }: { quizId: number; code?: string }) => {
        const response = await QuizService.get(quizId, code);
        return response.data;
    }
);

const fetchQuizByCode = createAsyncThunk<QuizViewModel, string, { rejectValue: number | undefined }>(
    'quiz/getByCode',
    async (quizCode, { rejectWithValue }) => {
        try {
            const response = await QuizService.getByCode(quizCode);
            return response.data;
        } catch (error) {
            return rejectWithValue(axios.isAxiosError(error) ? error.response?.status : undefined);
        }
    }
);

const createQuiz = createAsyncThunk(
    'quiz/create',
    async (data: CreateQuizPayload) => {
        const response = await QuizService.create(data);
        return response.data;
    }
);

const updateQuiz = createAsyncThunk(
    'quiz/update',
    async (data: UpdateQuizPayload) => {
        const response = await QuizService.Update(data);
        return response.data;
    }
);

const deleteQuiz = createAsyncThunk(
    'quiz/delete',
    async (quizId: number) => {
        const response = await QuizService.delete(quizId);
        return response.data;
    }
);

const submitQuiz = createAsyncThunk<QuizGameViewModel, SubmitQuizPayload, { rejectValue: number | undefined }>(
    'quiz/submit',
    async (data, { rejectWithValue }) => {
        try {
            const response = await QuizService.submit(data);
            return response.data;
        } catch (error) {
            return rejectWithValue(axios.isAxiosError(error) ? error.response?.status : undefined);
        }
    }
);

const getQuizAnalytics = createAsyncThunk(
    'quiz/getAnalytics',
    async (quizId: number) => {
        const response = await QuizService.getQuizAnalytics(quizId);
        return response.data;
    }
);

const fetchMyGames = createAsyncThunk(
    'quiz/getMyGames',
    async () => {
        const response = await QuizService.getMyGames();
        return response.data;
    }
);

const fetchQuizGame = createAsyncThunk<QuizGameReviewViewModel, number, { rejectValue: number | undefined }>(
    'quiz/getGame',
    async (gameId, { rejectWithValue }) => {
        try {
            const response = await QuizService.getGame(gameId);
            return response.data;
        } catch (error) {
            return rejectWithValue(axios.isAxiosError(error) ? error.response?.status : undefined);
        }
    }
);

const quizSlice = createSlice({
  name: "quiz",
  initialState,
  reducers: {
    resetSubmit: (state) => {
      state.lastGame = undefined;
      state.submitStatus = "idle";
    },
  },
  extraReducers: (builder) => {
    // Cleared first, so "no quizzes" is never said about a list another screen left here.
    builder.addCase(searchQuizzes.pending, (state) => {
      state.quizzes = undefined;
    });
    builder.addCase(fetchMyQuizzes.fulfilled, (state, action: PayloadAction<QuizBaseModel[]>) => {
        state.quizzes = action.payload;
    });
    builder.addCase(searchQuizzes.fulfilled, (state, action: PayloadAction<QuizBaseModel[]>) => {
        state.quizzes = action.payload;
    });
    // Cleared first, so a quiz that fails to load never shows the one opened before it.
    builder.addCase(fetchQuiz.pending, (state) => {
        state.quiz = undefined;
    });
    builder.addCase(fetchQuiz.fulfilled, (state, action: PayloadAction<QuizViewModel>) => {
        state.quiz = action.payload; 
    });
    // Cleared first, so one quiz's plays never show under another.
    builder.addCase(getQuizAnalytics.pending, (state) => {
        state.analytics = undefined;
        state.analyticsFailed = false;
    });
    builder.addCase(getQuizAnalytics.fulfilled, (state, action: PayloadAction<QuizAnalytics>) => {
        state.analytics = action.payload;
    });
    // An aborted request is one the screen has already replaced with another.
    builder.addCase(getQuizAnalytics.rejected, (state, action) => {
        if (!action.meta.aborted) {
            state.analyticsFailed = true;
        }
    });
    builder.addCase(fetchMyGames.pending, (state) => {
        state.myGames = undefined;
        state.myGamesFailed = false;
    });
    builder.addCase(fetchMyGames.fulfilled, (state, action: PayloadAction<MyQuizGames>) => {
        state.myGames = action.payload;
    });
    builder.addCase(fetchMyGames.rejected, (state, action) => {
        if (!action.meta.aborted) {
            state.myGamesFailed = true;
        }
    });
    // Cleared first, so one play's review never shows under another's address.
    builder.addCase(fetchQuizGame.pending, (state) => {
        state.gameReview = undefined;
        state.gameReviewFailed = false;
        state.gameReviewLocked = false;
    });
    builder.addCase(fetchQuizGame.fulfilled, (state, action: PayloadAction<QuizGameReviewViewModel>) => {
        state.gameReview = action.payload;
    });
    builder.addCase(fetchQuizGame.rejected, (state, action) => {
        if (action.payload === 409) {
            state.gameReviewLocked = true;
        } else if (!action.meta.aborted) {
            state.gameReviewFailed = true;
        }
    });
    builder.addCase(submitQuiz.pending, (state) => {
        state.submitStatus = "pending";
    });
    builder.addCase(submitQuiz.fulfilled, (state, action) => {
        state.lastGame = action.payload;
        state.submitStatus = "saved";
    });
    builder.addCase(submitQuiz.rejected, (state, action) => {
        const status = action.payload;
        state.submitStatus = status !== undefined && status >= 400 && status < 500 ? "rejected" : "failed";
    });
    builder.addCase(fetchQuizByCode.fulfilled, (state, action: PayloadAction<QuizViewModel>) => {
        state.quiz = action.payload;
    });
  }
});

export const { resetSubmit } = quizSlice.actions;

export {
    fetchMyQuizzes,
    createQuiz,
    updateQuiz,
    deleteQuiz,
    fetchQuiz,
    submitQuiz,
    getQuizAnalytics,
    fetchMyGames,
    fetchQuizGame,
    fetchQuizByCode,
    searchQuizzes,
};

export default quizSlice.reducer;
