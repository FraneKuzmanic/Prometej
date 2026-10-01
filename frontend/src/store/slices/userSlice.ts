import { PayloadAction, createAsyncThunk, createSlice } from "@reduxjs/toolkit";

import usersService from "../../services/routes/user";
import {LoginInput, UserCreateRequest, UserViewModel} from "../../types/models/User";

interface UserState {
  user: UserViewModel | undefined;
  authenticated: boolean | undefined;
  registered: boolean | undefined;
}

const initialState: UserState = {
  user: undefined,
  authenticated: undefined,
  registered: undefined,
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
  async (id: string) => {
    const response = await usersService.getUser(id);
    return response.data;
  }
);

const registerStudent = createAsyncThunk(
  'user/registerStudentStatus',
  async (data: UserCreateRequest) => {
    const response = await usersService.register(data);
    return response.data;
  }
);

const deleteCurrentUser = createAsyncThunk(
    'user/deleteCurrentUser',
    async (userid: number) => {
        await usersService.deleteUser(userid);
    }
);

const userSlice = createSlice({
  name: "user",
  initialState,
  reducers: {
    clearUser: (state) => {
      state.user = undefined;
      state.authenticated = undefined;
    },
    clearRegistered: (state) => {
      state.registered = undefined;
    },
  },
  extraReducers: (builder) => {
    builder.addCase(attemptLogin.fulfilled, (state, action: PayloadAction<UserViewModel>) => {
      state.user = action.payload;
      state.authenticated = true;
    }).addCase(attemptLogin.rejected, (state) => {
      state.authenticated = false;
    });
    builder.addCase(attemptLogout.fulfilled, (state) => {
      state.user = undefined;
      state.authenticated = undefined;
    });
    builder.addCase(fetchCurrentUser.fulfilled, (state, action: PayloadAction<UserViewModel>) => {
      state.user = action.payload;
      state.authenticated = true;
    });
    builder.addCase(registerStudent.fulfilled, (state) => {
      state.registered = true;
    }).addCase(registerStudent.rejected, (state) => {
      state.registered = false;
    })
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
