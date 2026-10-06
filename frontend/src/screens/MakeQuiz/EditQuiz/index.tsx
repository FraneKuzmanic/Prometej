import { useEffect } from "react";
import QuizEditor from "../../../components/QuizEditor";
import {
  EditorSourceTexts,
  EditorQuestion,
  fromQuiz,
  toRequest,
} from "../../../components/QuizEditor/questions";
import { fetchQuiz, updateQuiz } from "../../../store/slices/quizSlice";
import { QuizCreateRequest } from "../../../types/models/Quiz";
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
    header: QuizCreateRequest,
    questions: EditorQuestion[],
    sourceTexts: EditorSourceTexts
  ) => {
    const result = await dispatch(
      updateQuiz({
        quiz: { id: quiz.id, ...header },
        // The lists are the whole set: a stored Question or Source Text left out is removed.
        // A started Test's Questions cannot change, so only its header is sent.
        ...(quiz.questionsLocked ? {} : toRequest(questions, sourceTexts)),
      })
    );
    const saved = updateQuiz.fulfilled.match(result);
    if (saved) navigate("/my-quizzes");
    return saved;
  };

  const { questions, sourceTexts } = fromQuiz(quiz);

  return (
    <QuizEditor
      key={quiz.id}
      initialTitle={quiz.title}
      initialIsPrivate={quiz.isPrivate}
      initialPeriodId={quiz.periodId}
      initialIsTest={quiz.isTest}
      initialTimeLimitMinutes={quiz.timeLimitMinutes}
      initialClosesAt={quiz.closesAt}
      initialQuestions={questions}
      initialSourceTexts={sourceTexts}
      locked={quiz.questionsLocked}
      onSave={saveQuiz}
      onCancel={() => navigate("/my-quizzes")}
    />
  );
}
