import { PayloadAction, createAsyncThunk, createSlice } from "@reduxjs/toolkit";
import { isAxiosError } from "axios";

import periodService from "../../services/routes/period";
import { Period, PeriodContentViewModel, PeriodContentEditRequest, PeriodSearchContent } from "../../types/models/Period";

interface PeriodState {
    periods: Period[] | undefined;
    periodsFailed: boolean;
    // undefined while it loads, null when the Period has no content yet
    periodContent: PeriodContentViewModel | null | undefined;
    periodContentFailed: boolean;
    searchContent: PeriodSearchContent[] | undefined;
    searchFailed: boolean;
}

const initialState: PeriodState = {
    periods: undefined,
    periodsFailed: false,
    periodContent: undefined,
    periodContentFailed: false,
    searchContent: undefined,
    searchFailed: false,
};

const fetchPeriods = createAsyncThunk(
    'period/fetchPeriods',
    async () => {
        const response = await periodService.getAll();
        return response.data;
    }
);

const fetchPeriodContent = createAsyncThunk(
    'period/fetchPeriodContent',
    async (id: string) => {
        try {
            const response = await periodService.get(id);
            return response.data;
        } catch (error) {
            // The server's answer for a Period without content. Any other failure is rejected,
            // so the screen never takes a lost connection for an empty Period.
            if (isAxiosError(error) && error.response?.status === 404) {
                return null;
            }
            throw error;
        }
    }
);
const editPeriodContent = createAsyncThunk(
    'period/editPeriodContent',
    async (data: PeriodContentEditRequest) => {
        const response = await periodService.edit(data);
        return response.data;
    }
);

const searchPeriodContent = createAsyncThunk(
    'period/searchPeriodContent',
    async (query: string) => {
        const response = await periodService.search(query);
        return response.data;
    }
);

const periodSlice = createSlice({
  name: "period",
  initialState,
  reducers: {
    clearPeriodContent: (state) => {
      state.periodContent = undefined;
    },
  },
  extraReducers: (builder) => {
    // A list already loaded stays on screen while it is fetched again, and if that fails.
    builder.addCase(fetchPeriods.pending, (state) => {
      state.periodsFailed = false;
    });
    builder.addCase(fetchPeriods.fulfilled, (state, action: PayloadAction<Period[]>) => {
      state.periods = action.payload;
    });
    builder.addCase(fetchPeriods.rejected, (state) => {
      state.periodsFailed = true;
    });
    // Cleared first, so a Period with no content never shows (or saves over) the previous one's.
    builder.addCase(fetchPeriodContent.pending, (state) => {
      state.periodContent = undefined;
      state.periodContentFailed = false;
    });
    builder.addCase(fetchPeriodContent.fulfilled, (state, action: PayloadAction<PeriodContentViewModel | null>) => {
      state.periodContent = action.payload;
    });
    builder.addCase(fetchPeriodContent.rejected, (state) => {
      state.periodContentFailed = true;
    });
    // Cleared first, so one search never shows the results of the one before it.
    builder.addCase(searchPeriodContent.pending, (state) => {
      state.searchContent = undefined;
      state.searchFailed = false;
    });
    builder.addCase(searchPeriodContent.fulfilled, (state, action: PayloadAction<PeriodSearchContent[]>) => {
      state.searchContent = action.payload;
    });
    // An aborted request is one the screen has already replaced with another.
    builder.addCase(searchPeriodContent.rejected, (state, action) => {
      if (!action.meta.aborted) {
        state.searchFailed = true;
      }
    });
  }
});

export const {
    clearPeriodContent,
} = periodSlice.actions;


export {
    fetchPeriods,
    fetchPeriodContent,
    editPeriodContent,
    searchPeriodContent,
};

export default periodSlice.reducer;
