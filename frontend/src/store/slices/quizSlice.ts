import { PayloadAction, createAsyncThunk, createSlice } from "@reduxjs/toolkit";
import axios from "axios";

import QuizService from "../../services/routes/quiz";
import { AnswerCreateRequest, QuestionCreateRequest, QuestionEditRequest, QuizBaseModel, QuizCreateRequest, QuizEditRequest, QuizGameViewModel, QuizViewModel } from "../../types/models/Quiz";

interface QuizState {
    quizzes: QuizBaseModel[] | undefined;
    quiz: QuizViewModel | undefined;
    quizGames: QuizGameViewModel[] | undefined;
    analyticsFailed: boolean;
    // the Quiz Game the server stored for the play just finished
    lastGame: QuizGameViewModel | undefined;
    // "rejected": the server refused the submission, so sending it again cannot help.
    // "failed": the request did not get an answer, or got a server error.
    submitStatus: "idle" | "pending" | "saved" | "rejected" | "failed";
}

export interface CreateQuizPayload {
    quiz: QuizCreateRequest;
    questions: QuestionCreateRequest[];

}
export interface UpdateQuizPayload {
    quiz: QuizEditRequest;
    questions?: QuestionEditRequest[];

}

export interface SubmitQuizPayload {
    quizId: number;
    answers: AnswerCreateRequest[];
}

const initialState: QuizState = {
    quizzes: undefined,
    quiz: undefined,
    quizGames: undefined,
    analyticsFailed: false,
    lastGame: undefined,
    submitStatus: "idle",
};

const fetchAllPublicQuizzes = createAsyncThunk(
    'quiz/getAllPublicQuizzes',
    async () => {
        const response = await QuizService.getAll();
        return response.data;
    }
);
const fetchAllUserQuizzes = createAsyncThunk(
    'quiz/getAllUserQuizzes',
    async (userId: number) => {
        const response = await QuizService.getAllUserQuizzes(userId);
        return response.data;
    }
);

const searchQuizzes = createAsyncThunk(
    'quiz/search',
    async (query: string) => {
        const response = await QuizService.search(query);
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

const fetchQuizByCode = createAsyncThunk(
    'quiz/getByCode',
    async (quizCode: string) => {
        const response = await QuizService.getByCode(quizCode);
        return response.data;
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
    builder.addCase(fetchAllPublicQuizzes.pending, (state) => {
      state.quizzes = undefined;
    });
    builder.addCase(fetchAllPublicQuizzes.fulfilled, (state, action: PayloadAction<QuizBaseModel[]>) => {
      state.quizzes = action.payload;
    });
    builder.addCase(fetchAllUserQuizzes.fulfilled, (state, action: PayloadAction<QuizBaseModel[]>) => {
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
        state.quizGames = undefined;
        state.analyticsFailed = false;
    });
    builder.addCase(getQuizAnalytics.fulfilled, (state, action: PayloadAction<QuizGameViewModel[]>) => {
        state.quizGames = action.payload;
    });
    // An aborted request is one the screen has already replaced with another.
    builder.addCase(getQuizAnalytics.rejected, (state, action) => {
        if (!action.meta.aborted) {
            state.analyticsFailed = true;
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
    fetchAllPublicQuizzes,
    fetchAllUserQuizzes,
    createQuiz,
    updateQuiz,
    deleteQuiz,
    fetchQuiz,
    submitQuiz,
    getQuizAnalytics,
    fetchQuizByCode,
    searchQuizzes,
};

export default quizSlice.reducer;
