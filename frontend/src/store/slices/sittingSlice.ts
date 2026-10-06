import { createAsyncThunk, createSlice } from "@reduxjs/toolkit";
import axios from "axios";

import sittingService from "../../services/routes/sitting";
import { Sitting, SittingAnswerRequest, SittingInfo, SittingResult } from "../../types/models/Sitting";

interface SittingState {
    // undefined while it loads, null when the Quiz cannot be sat
    info: SittingInfo | null | undefined;
    infoFailed: boolean;
    // The Quiz the info and the Sitting were asked for. A screen that opens on another one
    // sees that what is stored is not its own, and a late answer for another Quiz is dropped.
    requestedQuizId: number | undefined;
    sitting: Sitting | undefined;
    // the outcome of the Sitting that ended on this screen
    result: SittingResult | undefined;
}

const initialState: SittingState = {
    info: undefined,
    infoFailed: false,
    requestedQuizId: undefined,
    sitting: undefined,
    result: undefined,
};

type Rejected = { rejectValue: number | undefined };

const statusOf = (error: unknown) => (axios.isAxiosError(error) ? error.response?.status : undefined);

const fetchSittingInfo = createAsyncThunk(
    'sitting/fetchInfo',
    async ({ quizId, code }: { quizId: number; code?: string }): Promise<SittingInfo | null> => {
        try {
            const response = await sittingService.info(quizId, code);
            return response.data;
        } catch (error) {
            // The server's answer for a Quiz that cannot be sat, a wrong Entry Code included.
            if (statusOf(error) === 404) {
                return null;
            }
            throw error;
        }
    }
);

// Answers with the caller's running Sitting if there is one, so it also resumes.
const startSitting = createAsyncThunk<Sitting, { quizId: number; code?: string }, Rejected>(
    'sitting/start',
    async ({ quizId, code }, { rejectWithValue }) => {
        try {
            const response = await sittingService.start(quizId, code);
            return response.data;
        } catch (error) {
            return rejectWithValue(statusOf(error));
        }
    }
);

const saveSittingAnswer = createAsyncThunk<void, { sittingId: number; answer: SittingAnswerRequest }, Rejected>(
    'sitting/saveAnswer',
    async ({ sittingId, answer }, { rejectWithValue }) => {
        try {
            await sittingService.saveAnswer(sittingId, answer);
        } catch (error) {
            return rejectWithValue(statusOf(error));
        }
    }
);

// Also the way to read the result of a Sitting the server has ended already.
const finishSitting = createAsyncThunk<SittingResult, number, Rejected>(
    'sitting/finish',
    async (sittingId, { rejectWithValue }) => {
        try {
            const response = await sittingService.finish(sittingId);
            return response.data;
        } catch (error) {
            return rejectWithValue(statusOf(error));
        }
    }
);

const sittingSlice = createSlice({
  name: "sitting",
  initialState,
  reducers: {},
  extraReducers: (builder) => {
    // Cleared first, so one Quiz never shows the Sitting or the result of the one before it.
    builder.addCase(fetchSittingInfo.pending, (state, action) => {
      state.info = undefined;
      state.infoFailed = false;
      state.sitting = undefined;
      state.result = undefined;
      state.requestedQuizId = action.meta.arg.quizId;
    });
    builder.addCase(fetchSittingInfo.fulfilled, (state, action) => {
      if (action.meta.arg.quizId === state.requestedQuizId) {
        state.info = action.payload;
      }
    });
    // An aborted request is one the screen has already replaced with another.
    builder.addCase(fetchSittingInfo.rejected, (state, action) => {
      if (!action.meta.aborted && action.meta.arg.quizId === state.requestedQuizId) {
        state.infoFailed = true;
      }
    });
    builder.addCase(startSitting.fulfilled, (state, action) => {
      if (action.meta.arg.quizId === state.requestedQuizId) {
        state.sitting = action.payload;
      }
    });
    builder.addCase(finishSitting.fulfilled, (state, action) => {
      if (action.meta.arg === state.sitting?.id) {
        state.result = action.payload;
        state.sitting = undefined;
      }
    });
  }
});

export {
    fetchSittingInfo,
    startSitting,
    saveSittingAnswer,
    finishSitting,
};

export default sittingSlice.reducer;
