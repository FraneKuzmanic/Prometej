import QuizEditor, { EditorQuestion } from "../../components/QuizEditor";
import { QuizCreateRequest } from "../../types/models/Quiz";
import { createQuiz } from "../../store/slices/quizSlice";
import { useAppDispatch } from "../../store/store";
import { useNavigate } from "react-router-dom";

export default function MakeQuiz() {
  const dispatch = useAppDispatch();
  const navigate = useNavigate();

  const generateRandomNumber = () => {
    return Math.floor(10000 + Math.random() * 90000);
  };

  const saveQuiz = (title: string, questions: EditorQuestion[]) => {
    const quiz: QuizCreateRequest = {
      title: title,
      isPrivate: true,
      entryCode: generateRandomNumber(),
    };
    return dispatch(createQuiz({ quiz: quiz, questions: questions })).then(
      () => navigate("/learning")
    );
  };

  return (
    <QuizEditor
      initialTitle=""
      initialQuestions={[]}
      onSave={saveQuiz}
      onCancel={() => navigate("/learning")}
    />
  );
}
