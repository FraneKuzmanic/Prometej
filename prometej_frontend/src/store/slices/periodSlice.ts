import { PayloadAction, createAsyncThunk, createSlice } from "@reduxjs/toolkit";

import periodService from "../../services/routes/period";
import { PeriodContentViewModel, PeriodContentEditRequest, PeriodSearchContent } from "../../types/models/Period";

interface PeriodState {
    periodContent: PeriodContentViewModel | undefined;
    searchContent: PeriodSearchContent[] | undefined;
}

const initialState: PeriodState = {
    periodContent: undefined,
    searchContent: undefined,
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
    builder.addCase(fetchPeriodContent.fulfilled, (state, action: PayloadAction<PeriodContentViewModel>) => {
      state.periodContent = action.payload;
      state.searchContent = undefined;
    });
    builder.addCase(searchPeriodContent.fulfilled, (state, action: PayloadAction<PeriodSearchContent[]>) => {
      state.searchContent = action.payload;
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
