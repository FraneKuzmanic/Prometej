import { PayloadAction, createAsyncThunk, createSlice } from "@reduxjs/toolkit";

import periodService from "../../services/routes/period";
import { PeriodContentViewModel, PeriodContentEditRequest, PeriodSearchContent } from "../../types/models/Period";

interface PeriodState {
    periodContent: PeriodContentViewModel | undefined;
    searchContent: PeriodSearchContent[] | undefined;
    searchFailed: boolean;
}

const initialState: PeriodState = {
    periodContent: undefined,
    searchContent: undefined,
    searchFailed: false,
};

const fetchPeriodContent = createAsyncThunk(
    'period/fetchPeriodContent',
    async (id: string) => {
        const response = await periodService.get(id);
        return response.data;
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
    // Cleared first, so a Period with no content never shows (or saves over) the previous one's.
    builder.addCase(fetchPeriodContent.pending, (state) => {
      state.periodContent = undefined;
    });
    builder.addCase(fetchPeriodContent.fulfilled, (state, action: PayloadAction<PeriodContentViewModel>) => {
      state.periodContent = action.payload;
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
    fetchPeriodContent,
    editPeriodContent,
    searchPeriodContent,
};

export default periodSlice.reducer;
