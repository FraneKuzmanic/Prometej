import { configureStore } from '@reduxjs/toolkit';
import { useDispatch } from 'react-redux';
import userSlice from './slices/userSlice';
import periodSlice from './slices/periodSlice';
import quizSlice from './slices/quizSlice';
import discussionSlice from './slices/discussionSlice';
import sittingSlice from './slices/sittingSlice';

export const store =  configureStore({
    reducer: {
        user: userSlice,
        period: periodSlice,
        quiz: quizSlice,
        discussion: discussionSlice,
        sitting: sittingSlice,
    },
});

export const useAppDispatch = () => useDispatch<typeof store.dispatch>();
export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;