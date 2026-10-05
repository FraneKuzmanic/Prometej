import { PayloadAction, createAsyncThunk, createSlice } from "@reduxjs/toolkit";
import axios from "axios";

import discussionService from "../../services/routes/discussion";
import { ReplyCreateRequest, Topic, TopicCreateRequest, TopicPage } from "../../types/models/Discussion";

interface DiscussionState {
    // undefined while it loads
    topicPage: TopicPage | undefined;
    topicPageFailed: boolean;
    // Which Period and page were asked for last. A screen that opens on another one sees
    // that the stored answer is not its own, before its own request has even started.
    topicPageOf: { periodId: number; page: number } | undefined;
    // undefined while it loads, null when there is no such Topic
    topic: Topic | null | undefined;
    topicFailed: boolean;
    topicOf: number | undefined;
}

const initialState: DiscussionState = {
    topicPage: undefined,
    topicPageFailed: false,
    topicPageOf: undefined,
    topic: undefined,
    topicFailed: false,
    topicOf: undefined,
};

const statusOf = (error: unknown) => (axios.isAxiosError(error) ? error.response?.status : undefined);

const fetchTopics = createAsyncThunk(
    'discussion/fetchTopics',
    async ({ periodId, page }: { periodId: number; page: number }) => {
        const response = await discussionService.getTopics(periodId, page);
        return response.data;
    }
);

// keep: the Topic on screen stays while it is read again after a Reply was added or deleted.
// Without it the screen would go blank for a moment on every Reply.
const fetchTopic = createAsyncThunk(
    'discussion/fetchTopic',
    async ({ id }: { id: number; keep?: boolean }) => {
        try {
            const response = await discussionService.getTopic(id);
            return response.data;
        } catch (error) {
            // The server's answer for a Topic that does not exist (or no longer does).
            if (statusOf(error) === 404) {
                return null;
            }
            throw error;
        }
    }
);

const createTopic = createAsyncThunk<number, { periodId: number; data: TopicCreateRequest }, { rejectValue: number | undefined }>(
    'discussion/createTopic',
    async ({ periodId, data }, { rejectWithValue }) => {
        try {
            const response = await discussionService.createTopic(periodId, data);
            return response.data;
        } catch (error) {
            return rejectWithValue(statusOf(error));
        }
    }
);

const createReply = createAsyncThunk<number, { topicId: number; data: ReplyCreateRequest }, { rejectValue: number | undefined }>(
    'discussion/createReply',
    async ({ topicId, data }, { rejectWithValue }) => {
        try {
            const response = await discussionService.createReply(topicId, data);
            return response.data;
        } catch (error) {
            return rejectWithValue(statusOf(error));
        }
    }
);

const deleteTopic = createAsyncThunk<void, number, { rejectValue: number | undefined }>(
    'discussion/deleteTopic',
    async (id, { rejectWithValue }) => {
        try {
            await discussionService.deleteTopic(id);
        } catch (error) {
            return rejectWithValue(statusOf(error));
        }
    }
);

const deleteReply = createAsyncThunk<void, number, { rejectValue: number | undefined }>(
    'discussion/deleteReply',
    async (id, { rejectWithValue }) => {
        try {
            await discussionService.deleteReply(id);
        } catch (error) {
            return rejectWithValue(statusOf(error));
        }
    }
);

const discussionSlice = createSlice({
  name: "discussion",
  initialState,
  reducers: {},
  extraReducers: (builder) => {
    // Cleared first, so one Period never shows the Topics of the one before it.
    builder.addCase(fetchTopics.pending, (state, action) => {
      state.topicPage = undefined;
      state.topicPageFailed = false;
      state.topicPageOf = action.meta.arg;
    });
    builder.addCase(fetchTopics.fulfilled, (state, action: PayloadAction<TopicPage>) => {
      state.topicPage = action.payload;
    });
    // An aborted request is one the screen has already replaced with another.
    builder.addCase(fetchTopics.rejected, (state, action) => {
      if (!action.meta.aborted) {
        state.topicPageFailed = true;
      }
    });
    builder.addCase(fetchTopic.pending, (state, action) => {
      if (!action.meta.arg.keep) {
        state.topic = undefined;
      }
      state.topicFailed = false;
      state.topicOf = action.meta.arg.id;
    });
    builder.addCase(fetchTopic.fulfilled, (state, action: PayloadAction<Topic | null>) => {
      state.topic = action.payload;
    });
    builder.addCase(fetchTopic.rejected, (state, action) => {
      if (!action.meta.aborted) {
        state.topicFailed = true;
      }
    });
  }
});

export {
    fetchTopics,
    fetchTopic,
    createTopic,
    createReply,
    deleteTopic,
    deleteReply,
};

export default discussionSlice.reducer;
