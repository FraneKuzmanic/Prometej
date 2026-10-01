import ProtectedRoute from "../components/ProtectedRoute";
import HomePage from "./HomePage";
import Learning from "./Learning";
import Login from "./Login";
import MyQuizzes from "./MyQuizzes";
import Quizzes from "./Quizzes";
import MakeQuiz from "./MakeQuiz";
import Register from "./Register";
import Period from "./Period";
import Search from "./Search";
import { Navigate, createBrowserRouter } from "react-router-dom";
import EditQuiz from "./MakeQuiz/EditQuiz";
import PlayQuiz from "./PlayQuiz";
import { QuizDetails } from "./QuizDetails";

export const appRouter = createBrowserRouter([
  {
    path: "/",
    element: <Navigate to="/learning" replace />,
  },
  {
    path: "/",
    element: (
      <ProtectedRoute>
        <HomePage />
      </ProtectedRoute>
    ),
    children: [
      { path: "learning", element: <Learning /> },
      { path: "search", element: <Search /> },
      { path: "learning/:id", element: <Period /> },
      { path: "quizzes", element: <Quizzes /> },
      { path: "my-quizzes", element: <MyQuizzes /> },
      { path: "quiz-details/:id", element: <QuizDetails /> },
      // other routes...
    ],
  },
  {
    path: "/register",
    element: <Register />,
  },
  {
    path: "/login",
    element: <Login />,
  },
  {
    path: "/play-quiz/:id",
    element: <PlayQuiz />,
  },
  {
    path: "/make-quiz",
    element: <MakeQuiz />,
  },
  {
    path: "/edit-quiz/:id",
    element: <EditQuiz />,
  },
]);

export default appRouter;
