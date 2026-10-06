import QuizEditor from "../../components/QuizEditor";
import {
  EditorSourceTexts,
  EditorQuestion,
  toRequest,
} from "../../components/QuizEditor/questions";
import { createQuiz } from "../../store/slices/quizSlice";
import { QuizCreateRequest } from "../../types/models/Quiz";
import { useAppDispatch } from "../../store/store";
import { useNavigate } from "react-router-dom";

export default function MakeQuiz() {
  const dispatch = useAppDispatch();
  const navigate = useNavigate();

  const saveQuiz = async (
    quiz: QuizCreateRequest,
    questions: EditorQuestion[],
    sourceTexts: EditorSourceTexts
  ) => {
    const result = await dispatch(
      createQuiz({ quiz, ...toRequest(questions, sourceTexts) })
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
      initialIsTest={false}
      initialTimeLimitMinutes={null}
      initialClosesAt={null}
      initialQuestions={[]}
      initialSourceTexts={{}}
      onSave={saveQuiz}
      onCancel={() => navigate("/learning")}
    />
  );
}
