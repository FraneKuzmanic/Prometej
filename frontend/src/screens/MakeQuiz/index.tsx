import QuizEditor from "../../components/QuizEditor";
import {
  EditorPassages,
  EditorQuestion,
  toRequest,
} from "../../components/QuizEditor/questions";
import { createQuiz } from "../../store/slices/quizSlice";
import { useAppDispatch } from "../../store/store";
import { useNavigate } from "react-router-dom";

export default function MakeQuiz() {
  const dispatch = useAppDispatch();
  const navigate = useNavigate();

  const saveQuiz = async (
    title: string,
    isPrivate: boolean,
    periodId: number | null,
    questions: EditorQuestion[],
    passages: EditorPassages
  ) => {
    const result = await dispatch(
      createQuiz({
        quiz: { title, isPrivate, periodId },
        ...toRequest(questions, passages),
      })
    );
    const saved = createQuiz.fulfilled.match(result);
    if (saved) navigate("/learning");
    return saved;
  };

  return (
    <QuizEditor
      initialTitle=""
      initialIsPrivate={true}
      initialPeriodId={null}
      initialQuestions={[]}
      initialPassages={{}}
      onSave={saveQuiz}
      onCancel={() => navigate("/learning")}
    />
  );
}
