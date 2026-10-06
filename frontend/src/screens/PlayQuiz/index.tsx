import { useEffect, useState } from "react";
import {
  AnswerCreateRequest,
  QuestionViewModel,
} from "../../types/models/Quiz";
import { useSelector } from "react-redux";
import { RootState, useAppDispatch } from "../../store/store";
import { useNavigate, useParams, useSearchParams } from "react-router-dom";
import {
  fetchQuiz,
  resetSubmit,
  submitQuiz,
} from "../../store/slices/quizSlice";
import {
  Alert,
  Box,
  Button,
  IconButton,
  LinearProgress,
  Paper,
  Typography,
} from "@mui/material";
import ChoiceQuestion from "./ChoiceQuestion";
import MatchingQuestion from "./MatchingQuestion";
import OrderingQuestion from "./OrderingQuestion";
import SourceTextPanel from "./SourceTextPanel";
import { pointsLabel } from "../../types/points";
import "./styles.css";

export default function PlayQuiz() {
  const [quizQuestions, setQuizQuestions] = useState<QuestionViewModel[]>([]);
  const [quizAnswers, setQuizAnswers] = useState<AnswerCreateRequest[]>([]);
  const { quiz, lastGame, submitStatus } = useSelector(
    (state: RootState) => state.quiz
  );
  const { user, authenticated } = useSelector((state: RootState) => state.user);
  const [currentQuestion, setCurrentQuestion] = useState<QuestionViewModel>();
  const [currentQuestionNo, setCurrentQuestionNo] = useState(0);
  const [currentQuestionAnswered, setQuestionAnswered] = useState(false);
  const [totalQuestionNo, setTotalQuestionNo] = useState(0);
  const [score, setScore] = useState(0);
  // the points the answered Questions could give
  const [maxScore, setMaxScore] = useState(0);
  const [showScore, setShowScore] = useState(false);
  // Opening the Hint is the Student's choice; it does not change the Score.
  const [hintShown, setHintShown] = useState(false);
  const [submissionKey, setSubmissionKey] = useState("");
  const dispatch = useAppDispatch();
  const navigate = useNavigate();
  const { id } = useParams();
  const [searchParams] = useSearchParams();
  const code = searchParams.get("code") ?? undefined;

  // The server does not record a Creator's play of their own Quiz.
  const isCreator = !!user && !!quiz && user.id === quiz.creatorId;

  useEffect(() => {
    // A new play must not start with the result of the one before it.
    dispatch(resetSubmit());
    // One key per play, so "Pokušaj ponovno" cannot store the play a second time.
    setSubmissionKey(crypto.randomUUID());
    dispatch(fetchQuiz({ quizId: Number(id), code }));
  }, [dispatch, id, code]);

  // A Test is sat, not played: this address only leads there. Not before the session is
  // known, or its Creator, who may try it out here, would be sent on as well.
  const sitsInstead =
    !!quiz && quiz.id === Number(id) && quiz.isTest && authenticated !== undefined && !isCreator;

  useEffect(() => {
    if (sitsInstead) {
      navigate(`/sitting/${id}${code ? `?code=${code}` : ""}`, { replace: true });
    }
  }, [sitsInstead, navigate, id, code]);

  useEffect(() => {
    if (quiz) {
      setQuizQuestions(quiz.questions);
      setCurrentQuestion(quiz.questions[0]);
      setTotalQuestionNo(quiz.questions.length);
      setCurrentQuestionNo(0);
    }
  }, [quiz]);

  const handleMoveForward = () => {
    setCurrentQuestion(quizQuestions[currentQuestionNo + 1]);
    setCurrentQuestionNo(currentQuestionNo + 1);
    setQuestionAnswered(false);
    setHintShown(false);
  };

  const handleAnswered = (
    answer: AnswerCreateRequest,
    points: number,
    maxPoints: number
  ) => {
    setScore(score + points);
    setMaxScore(maxScore + maxPoints);
    setQuestionAnswered(true);
    setQuizAnswers([...quizAnswers, answer]);
  };

  const submit = () => {
    if (quiz) {
      dispatch(
        submitQuiz({ quizId: quiz.id, answers: quizAnswers, submissionKey })
      );
    }
  };

  const handleShowScore = () => {
    if (authenticated && !isCreator) {
      submit();
    }
    setCurrentQuestion(undefined);
    setShowScore(true);
  };

  const sourceText =
    quiz && currentQuestion
      ? quiz.sourceTexts.find(
          (text) => text.id === currentQuestion.sourceTextId
        )
      : undefined;

  // The stored Quiz Game is the result once there is one: an Answer row is a point.
  const savedGame = submitStatus === "saved" ? lastGame : undefined;
  const finalMaxScore = savedGame ? savedGame.answers.length : maxScore;

  // Signed-out comes first: a session that expired during the play ends up there too.
  const resultNote = () => {
    if (authenticated === false) {
      return "Prijavite se kako bi vaš rezultat bio spremljen.";
    }
    if (isCreator) {
      return "Ovo je vaš kviz, pa se rezultat ne sprema.";
    }
    switch (submitStatus) {
      case "saved":
        return "Rezultat je spremljen.";
      case "rejected":
        return "Rezultat nije spremljen jer je kviz u međuvremenu izmijenjen ili obrisan.";
      case "failed":
        return "Rezultat nije spremljen. Pokušajte ponovno.";
      default:
        return "";
    }
  };

  return (
    <Box className="play-quiz-screen-wrapper">
      {/* Not without the Quiz: a Quiz left in the store by an earlier play is copied into
          this screen's state before the fetch for this address clears it. */}
      {quiz && currentQuestion && (
        <Paper
          elevation={3}
          className={`quiz-play-container${
            sourceText ? " with-source-text" : ""
          }`}
        >
          <Box className="quiz-play-header">
            <Typography className="quiz-play-title" variant="h5">
              {quiz ? quiz.title : ""}
            </Typography>
            <LinearProgress
              variant="determinate"
              value={((currentQuestionNo + 1) / totalQuestionNo) * 100}
            />
          </Box>
          {/* Keyed by the text, so it stays as the Student left it through its Questions. */}
          {sourceText && (
            <SourceTextPanel key={sourceText.id} sourceText={sourceText} />
          )}
          <Box className="quiz-play-content">
            <Typography variant="h4" className="quiz-play-question">
              {currentQuestionNo + 1}. {currentQuestion.questionTitle}
            </Typography>
            {/* The key gives every Question its own component, so no answer carries over. */}
            {currentQuestion.type === "choice" && (
              <ChoiceQuestion
                key={currentQuestion.id}
                question={currentQuestion}
                onAnswered={handleAnswered}
              />
            )}
            {currentQuestion.type === "matching" && (
              <MatchingQuestion
                key={currentQuestion.id}
                question={currentQuestion}
                onAnswered={handleAnswered}
              />
            )}
            {currentQuestion.type === "ordering" && (
              <OrderingQuestion
                key={currentQuestion.id}
                question={currentQuestion}
                onAnswered={handleAnswered}
              />
            )}
            {currentQuestion.hintText && (
              <Box className="quiz-play-hint-row">
                <IconButton
                  aria-label="Prikaži pomoć"
                  aria-expanded={hintShown}
                  onClick={() => setHintShown(!hintShown)}
                >
                  <img width={30} height={30} src="/hint-icon.png" alt="" />
                </IconButton>
                {hintShown && (
                  <Alert severity="info" icon={false} className="quiz-play-hint">
                    {currentQuestion.hintText}
                  </Alert>
                )}
              </Box>
            )}
            {currentQuestionAnswered && currentQuestion.exploreMore && (
              <Box className="quiz-play-explore">
                <Typography variant="subtitle2">Saznaj više</Typography>
                <Typography>{currentQuestion.exploreMore}</Typography>
              </Box>
            )}
          </Box>
          <Box className="quiz-play-footer">
            <Typography>
              {currentQuestionNo + 1}. od {totalQuestionNo}
            </Typography>
            {currentQuestionNo + 1 === totalQuestionNo
              ? currentQuestionAnswered && (
                  <Button
                    onClick={() => handleShowScore()}
                    disabled={!currentQuestionAnswered}
                    variant="contained"
                  >
                    Završi
                  </Button>
                )
              : currentQuestionAnswered && (
                  <Button
                    onClick={() => handleMoveForward()}
                    variant="contained"
                  >
                    Dalje
                  </Button>
                )}
          </Box>
        </Paper>
      )}
      {quiz && quiz.questions.length === 0 && !quiz.isTest && (
        <Paper elevation={3} className="quiz-play-container">
          <Box className="quiz-play-header">
            <Typography className="quiz-play-title" variant="h5">
              {quiz.title}
            </Typography>
          </Box>
          <Box className="quiz-play-score">
            <Typography component="div" variant="h4">
              Ovaj kviz još nema pitanja.
            </Typography>
          </Box>
          <Box className="quiz-play-footer" sx={{ justifyContent: "flex-end" }}>
            <Button onClick={() => navigate("/")} variant="contained">
              Vrati na početnu
            </Button>
          </Box>
        </Paper>
      )}
      {showScore && (
        <Paper elevation={3} className="quiz-play-container">
          <Box className="quiz-play-header">
            <Typography className="quiz-play-title" variant="h5">
              {quiz ? quiz.title : ""}
            </Typography>
            <LinearProgress
              variant="determinate"
              value={((currentQuestionNo + 1) / totalQuestionNo) * 100}
            />
          </Box>
          <Box className="quiz-play-score">
            <Typography component="div" variant="h4">
              Vaš ukupni rezultat :
            </Typography>
            <Typography component="div" sx={{ marginTop: 5 }} variant="h2">
              {savedGame ? savedGame.score : score} / {finalMaxScore}{" "}
              {pointsLabel(finalMaxScore)}
            </Typography>
            <Typography component="div" sx={{ marginTop: 3 }}>
              {resultNote()}
            </Typography>
            {submitStatus === "failed" && (
              <Button sx={{ marginTop: 1 }} onClick={() => submit()}>
                Pokušaj ponovno
              </Button>
            )}
            {submitStatus === "saved" && lastGame && (
              <Button
                sx={{ marginTop: 1 }}
                onClick={() => navigate(`/my-results/${lastGame.id}`)}
              >
                Pregledaj odgovore
              </Button>
            )}
          </Box>
          <Box className="quiz-play-footer">
            <Typography>
              {currentQuestionNo + 1}. od {totalQuestionNo}
            </Typography>
            <Box sx={{ display: "flex", gap: 1 }}>
              {quiz?.periodId && (
                <Button
                  onClick={() => navigate(`/learning/${quiz.periodId}`)}
                  variant="outlined"
                >
                  Ponovi gradivo: {quiz.periodName}
                </Button>
              )}
              <Button onClick={() => navigate("/")} variant="contained">
                Vrati na početnu
              </Button>
            </Box>
          </Box>
        </Paper>
      )}
    </Box>
  );
}
