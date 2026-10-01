import { useEffect } from "react";
import QuizEditor, { EditorQuestion } from "../../../components/QuizEditor";
import { QuizEditRequest } from "../../../types/models/Quiz";
import { fetchQuiz, updateQuiz } from "../../../store/slices/quizSlice";
import { useSelector } from "react-redux";
import { RootState, useAppDispatch } from "../../../store/store";
import { useNavigate, useParams } from "react-router-dom";

export default function EditQuiz() {
  const { quiz } = useSelector((state: RootState) => state.quiz);
  const dispatch = useAppDispatch();
  const navigate = useNavigate();
  const { id } = useParams();

  useEffect(() => {
    dispatch(fetchQuiz({ quizId: Number(id) }));
  }, [dispatch, id]);

  // The editor copies its props into state once, so it mounts only when the Quiz is here.
  if (!quiz) {
    return null;
  }

  const saveQuiz = (title: string, questions: EditorQuestion[]) => {
    const updatedQuiz: QuizEditRequest = {
      id: quiz.id,
      title: title,
      isPrivate: quiz.isPrivate,
      entryCode: quiz.entryCode || undefined,
    };
    return dispatch(
      updateQuiz({
        quiz: updatedQuiz,
        questions: questions.map((question) => ({
          ...question,
          id: question.id ?? 0,
        })),
      })
    ).then(() => navigate("/my-quizzes"));
  };

  return (
    <QuizEditor
      key={quiz.id}
      initialTitle={quiz.title}
      initialQuestions={quiz.questions}
      onSave={saveQuiz}
      onCancel={() => navigate("/my-quizzes")}
    />
  );
}
