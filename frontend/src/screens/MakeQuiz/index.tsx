import QuizEditor, { EditorQuestion } from "../../components/QuizEditor";
import { createQuiz } from "../../store/slices/quizSlice";
import { useAppDispatch } from "../../store/store";
import { useNavigate } from "react-router-dom";

export default function MakeQuiz() {
  const dispatch = useAppDispatch();
  const navigate = useNavigate();

  const saveQuiz = async (
    title: string,
    isPrivate: boolean,
    questions: EditorQuestion[]
  ) => {
    const result = await dispatch(
      createQuiz({ quiz: { title, isPrivate }, questions })
    );
    const saved = createQuiz.fulfilled.match(result);
    if (saved) navigate("/learning");
    return saved;
  };

  return (
    <QuizEditor
      initialTitle=""
      initialIsPrivate={true}
      initialQuestions={[]}
      onSave={saveQuiz}
      onCancel={() => navigate("/learning")}
    />
  );
}
