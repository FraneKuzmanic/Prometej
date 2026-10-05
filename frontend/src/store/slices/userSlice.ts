import { PayloadAction, createAsyncThunk, createSlice } from "@reduxjs/toolkit";
import axios from "axios";

import usersService from "../../services/routes/user";
import {LoginInput, UserAccount, UserCreateRequest, UserNameEditRequest, UserPasswordEditRequest, UserViewModel} from "../../types/models/User";
import ROLE from "../../types/enums/Role";

interface UserState {
  user: UserViewModel | undefined;
  // undefined until the server has answered whether there is a session
  authenticated: boolean | undefined;
  loginFailed: boolean;
  registered: boolean | undefined;
  registerError: "conflict" | "other" | undefined;
  // the Admin's list of Users
  users: UserAccount[] | undefined;
  usersFailed: boolean;
}

const initialState: UserState = {
  user: undefined,
  authenticated: undefined,
  loginFailed: false,
  registered: undefined,
  registerError: undefined,
  users: undefined,
  usersFailed: false,
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

const fetchCurrentUser = createAsyncThunk<UserViewModel, void, { rejectValue: number | undefined }>(
  'user/checkCurrentUserStatus',
  async (_, { rejectWithValue }) => {
    try {
      const response = await usersService.getUser();
      return response.data;
    } catch (error) {
      return rejectWithValue(axios.isAxiosError(error) ? error.response?.status : undefined);
    }
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

const deleteCurrentUser = createAsyncThunk<void, void, { rejectValue: number | undefined }>(
    'user/deleteCurrentUser',
    async (_, { rejectWithValue }) => {
        try {
            await usersService.deleteUser();
        } catch (error) {
            return rejectWithValue(axios.isAxiosError(error) ? error.response?.status : undefined);
        }
    }
);

const updateName = createAsyncThunk<void, UserNameEditRequest, { rejectValue: number | undefined }>(
    'user/updateName',
    async (data, { rejectWithValue }) => {
        try {
            await usersService.updateName(data);
        } catch (error) {
            return rejectWithValue(axios.isAxiosError(error) ? error.response?.status : undefined);
        }
    }
);

const changePassword = createAsyncThunk<void, UserPasswordEditRequest, { rejectValue: number | undefined }>(
    'user/changePassword',
    async (data, { rejectWithValue }) => {
        try {
            await usersService.changePassword(data);
        } catch (error) {
            return rejectWithValue(axios.isAxiosError(error) ? error.response?.status : undefined);
        }
    }
);

const fetchUsers = createAsyncThunk(
    'user/getUsers',
    async () => {
        const response = await usersService.getUsers();
        return response.data;
    }
);

const setUserRole = createAsyncThunk<void, { id: number; role: ROLE }, { rejectValue: number | undefined }>(
    'user/setRole',
    async ({ id, role }, { rejectWithValue }) => {
        try {
            await usersService.setRole(id, role);
        } catch (error) {
            return rejectWithValue(axios.isAxiosError(error) ? error.response?.status : undefined);
        }
    }
);

const signOut = (state: UserState) => {
  state.user = undefined;
  state.authenticated = false;
  state.users = undefined;
};

const userSlice = createSlice({
  name: "user",
  initialState,
  reducers: {
    clearUser: signOut,
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
    builder.addCase(attemptLogout.fulfilled, signOut).addCase(attemptLogout.rejected, signOut);
    builder.addCase(fetchCurrentUser.fulfilled, (state, action: PayloadAction<UserViewModel>) => {
      state.user = action.payload;
      state.authenticated = true;
    }).addCase(fetchCurrentUser.rejected, (state, action) => {
      // A check that failed for another reason says nothing about a session already known.
      if (action.payload === 401 || state.authenticated === undefined) {
        signOut(state);
      }
    });
    builder.addCase(registerStudent.pending, (state) => {
      state.registerError = undefined;
    }).addCase(registerStudent.fulfilled, (state) => {
      state.registered = true;
    }).addCase(registerStudent.rejected, (state, action) => {
      state.registerError = action.payload === 409 ? "conflict" : "other";
    });
    builder.addCase(deleteCurrentUser.fulfilled, signOut);
    builder.addCase(updateName.fulfilled, (state, action) => {
      if (state.user) {
        state.user.firstName = action.meta.arg.firstName;
        state.user.lastName = action.meta.arg.lastName;
      }
    });
    builder.addCase(fetchUsers.pending, (state) => {
      state.users = undefined;
      state.usersFailed = false;
    }).addCase(fetchUsers.fulfilled, (state, action: PayloadAction<UserAccount[]>) => {
      state.users = action.payload;
    }).addCase(fetchUsers.rejected, (state, action) => {
      if (!action.meta.aborted) {
        state.usersFailed = true;
      }
    });
    builder.addCase(setUserRole.fulfilled, (state, action) => {
      const account = state.users?.find((u) => u.id === action.meta.arg.id);
      if (account) {
        account.role = action.meta.arg.role;
      }
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
  updateName,
  changePassword,
  fetchUsers,
  setUserRole,
};

export default userSlice.reducer;
