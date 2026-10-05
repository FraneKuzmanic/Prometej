import RequireRole from "../components/RequireRole";
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
import MyResults from "./MyResults";
import GameReview from "./MyResults/GameReview";
import ROLE from "../types/enums/Role";

const quizCreatorRoles = [ROLE.Teacher, ROLE.Admin];
// Signed in is enough: anyone who plays has results.
const anyRole = [ROLE.Student, ROLE.Teacher, ROLE.Admin];

export const appRouter = createBrowserRouter([
  {
    path: "/",
    element: <Navigate to="/learning" replace />,
  },
  {
    path: "/",
    element: <HomePage />,
    children: [
      { path: "learning", element: <Learning /> },
      { path: "search", element: <Search /> },
      { path: "learning/:id", element: <Period /> },
      { path: "quizzes", element: <Quizzes /> },
      {
        path: "my-quizzes",
        element: (
          <RequireRole roles={quizCreatorRoles}>
            <MyQuizzes />
          </RequireRole>
        ),
      },
      {
        path: "quiz-details/:id",
        element: (
          <RequireRole roles={quizCreatorRoles}>
            <QuizDetails />
          </RequireRole>
        ),
      },
      {
        path: "my-results",
        element: (
          <RequireRole roles={anyRole}>
            <MyResults />
          </RequireRole>
        ),
      },
      {
        path: "my-results/:id",
        element: (
          <RequireRole roles={anyRole}>
            <GameReview />
          </RequireRole>
        ),
      },
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
    element: (
      <RequireRole roles={quizCreatorRoles}>
        <MakeQuiz />
      </RequireRole>
    ),
  },
  {
    path: "/edit-quiz/:id",
    element: (
      <RequireRole roles={quizCreatorRoles}>
        <EditQuiz />
      </RequireRole>
    ),
  },
]);

export default appRouter;
