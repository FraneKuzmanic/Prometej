import { PayloadAction, createAsyncThunk, createSlice } from "@reduxjs/toolkit";

import QuizService from "../../services/routes/quiz";
import { AnswerCreateRequest, QuestionCreateRequest, QuestionEditRequest, QuizBaseModel, QuizCreateRequest, QuizEditRequest, QuizGameCreateRequest, QuizGameViewModel, QuizViewModel } from "../../types/models/Quiz";

interface QuizState {
    quizzes: QuizBaseModel[] | undefined;
    quiz: QuizViewModel | undefined;
    quizGames: QuizGameViewModel[] | undefined;
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
    quizGame: QuizGameCreateRequest;
    answers: AnswerCreateRequest[];
}

const initialState: QuizState = {
    quizzes: undefined,
    quiz: undefined,
    quizGames: undefined,
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
    async (quizId: number) => {
        const response = await QuizService.get(quizId);
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

const submitQuiz = createAsyncThunk(
    'quiz/submit',
    async (data: SubmitQuizPayload) => {
        const response = await QuizService.submit(data);
        return response.data;
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
  },
  extraReducers: (builder) => {
    builder.addCase(fetchAllPublicQuizzes.fulfilled, (state, action: PayloadAction<QuizBaseModel[]>) => {
      state.quizzes = action.payload;
    });
    builder.addCase(fetchAllUserQuizzes.fulfilled, (state, action: PayloadAction<QuizBaseModel[]>) => {
        state.quizzes = action.payload;
    });
    builder.addCase(searchQuizzes.fulfilled, (state, action: PayloadAction<QuizBaseModel[]>) => {
        state.quizzes = action.payload;
    });
    builder.addCase(fetchQuiz.fulfilled, (state, action: PayloadAction<QuizViewModel>) => {
        state.quiz = action.payload; 
    });
    builder.addCase(getQuizAnalytics.fulfilled, (state, action: PayloadAction<QuizGameViewModel[]>) => {
        state.quizGames = action.payload;
    });
    builder.addCase(fetchQuizByCode.fulfilled, (state, action: PayloadAction<QuizViewModel>) => {
        state.quiz = action.payload;
    });
  }
});

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
