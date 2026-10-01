import { PayloadAction, createAsyncThunk, createSlice } from "@reduxjs/toolkit";
import axios from "axios";

import usersService from "../../services/routes/user";
import {LoginInput, UserCreateRequest, UserViewModel} from "../../types/models/User";

interface UserState {
  user: UserViewModel | undefined;
  // undefined until the server has answered whether there is a session
  authenticated: boolean | undefined;
  loginFailed: boolean;
  registered: boolean | undefined;
  registerError: "conflict" | "other" | undefined;
}

const initialState: UserState = {
  user: undefined,
  authenticated: undefined,
  loginFailed: false,
  registered: undefined,
  registerError: undefined,
};

const attemptLogin = createAsyncThunk(
  'user/loginStatus',
  async (user: LoginInput) => {
    const response = await usersService.login(user);
    return response.data;
  }
);

const attemptLogout = createAsyncThunk(
  'user/logoutStatus',
  async () => {
    const response = await usersService.logout();
    return response.data;
  }
);

const fetchCurrentUser = createAsyncThunk(
  'user/checkCurrentUserStatus',
  async () => {
    const response = await usersService.getUser();
    return response.data;
  }
);

const registerStudent = createAsyncThunk<number, UserCreateRequest, { rejectValue: number | undefined }>(
  'user/registerStudentStatus',
  async (data, { rejectWithValue }) => {
    try {
      const response = await usersService.register(data);
      return response.data;
    } catch (error) {
      return rejectWithValue(axios.isAxiosError(error) ? error.response?.status : undefined);
    }
  }
);

const deleteCurrentUser = createAsyncThunk(
    'user/deleteCurrentUser',
    async () => {
        await usersService.deleteUser();
    }
);

const userSlice = createSlice({
  name: "user",
  initialState,
  reducers: {
    clearUser: (state) => {
      state.user = undefined;
      state.authenticated = false;
    },
    clearRegistered: (state) => {
      state.registered = undefined;
    },
  },
  extraReducers: (builder) => {
    builder.addCase(attemptLogin.pending, (state) => {
      state.loginFailed = false;
    }).addCase(attemptLogin.fulfilled, (state, action: PayloadAction<UserViewModel>) => {
      state.user = action.payload;
      state.authenticated = true;
    }).addCase(attemptLogin.rejected, (state) => {
      state.loginFailed = true;
    });
    // Signed out locally even if the request failed: the user asked to leave.
    builder.addCase(attemptLogout.fulfilled, (state) => {
      state.user = undefined;
      state.authenticated = false;
    }).addCase(attemptLogout.rejected, (state) => {
      state.user = undefined;
      state.authenticated = false;
    });
    builder.addCase(fetchCurrentUser.fulfilled, (state, action: PayloadAction<UserViewModel>) => {
      state.user = action.payload;
      state.authenticated = true;
    }).addCase(fetchCurrentUser.rejected, (state) => {
      state.user = undefined;
      state.authenticated = false;
    });
    builder.addCase(registerStudent.pending, (state) => {
      state.registerError = undefined;
    }).addCase(registerStudent.fulfilled, (state) => {
      state.registered = true;
    }).addCase(registerStudent.rejected, (state, action) => {
      state.registerError = action.payload === 409 ? "conflict" : "other";
    });
    builder.addCase(deleteCurrentUser.fulfilled, (state) => {
      state.user = undefined;
      state.authenticated = false;
    });
  }
});

export const {
  clearUser,
  clearRegistered,
} = userSlice.actions;

export {
  attemptLogin,
  attemptLogout,
  fetchCurrentUser,
  registerStudent,
  deleteCurrentUser,
};

export default userSlice.reducer;
