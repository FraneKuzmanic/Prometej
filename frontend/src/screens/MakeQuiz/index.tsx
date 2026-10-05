import QuizEditor from "../../components/QuizEditor";
import {
  EditorSourceTexts,
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
    sourceTexts: EditorSourceTexts
  ) => {
    const result = await dispatch(
      createQuiz({
        quiz: { title, isPrivate, periodId },
        ...toRequest(questions, sourceTexts),
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
      initialSourceTexts={{}}
      onSave={saveQuiz}
      onCancel={() => navigate("/learning")}
    />
  );
}
