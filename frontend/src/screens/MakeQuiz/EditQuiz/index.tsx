import { useEffect } from "react";
import QuizEditor, { EditorQuestion } from "../../../components/QuizEditor";
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

  const saveQuiz = async (
    title: string,
    isPrivate: boolean,
    questions: EditorQuestion[]
  ) => {
    const result = await dispatch(
      updateQuiz({
        quiz: { id: quiz.id, title, isPrivate },
        // The list is the whole set: a stored Question left out of it is removed.
        questions: questions.map((question) => ({
          ...question,
          id: question.id ?? 0,
        })),
      })
    );
    const saved = updateQuiz.fulfilled.match(result);
    if (saved) navigate("/my-quizzes");
    return saved;
  };

  return (
    <QuizEditor
      key={quiz.id}
      initialTitle={quiz.title}
      initialIsPrivate={quiz.isPrivate}
      initialQuestions={quiz.questions.map((question) => ({
        ...question,
        hintText: question.hintText ?? "",
        exploreMore: question.exploreMore ?? "",
      }))}
      onSave={saveQuiz}
      onCancel={() => navigate("/my-quizzes")}
    />
  );
}
