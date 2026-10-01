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
  Box,
  Button,
  LinearProgress,
  List,
  ListItemText,
  Paper,
  Typography,
} from "@mui/material";
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
  const [showScore, setShowScore] = useState(false);
  const [currentAnswer, setCurrentAnswer] = useState<string>("");
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
    dispatch(fetchQuiz({ quizId: Number(id), code }));
  }, [dispatch, id, code]);

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
    setCurrentAnswer("");
  };

  const handleAnswer = (answer: string) => {
    if (currentQuestion) {
      const isCorrect = answer === currentQuestion.correctAnswer;
      setScore(score + (isCorrect ? 1 : 0));
      setCurrentAnswer(answer);
      setQuestionAnswered(true);
      setQuizAnswers([
        ...quizAnswers,
        {
          questionId: currentQuestion.id,
          answerText: answer,
        },
      ]);
    }
  };

  const submit = () => {
    if (quiz) {
      dispatch(submitQuiz({ quizId: quiz.id, answers: quizAnswers }));
    }
  };

  const handleShowScore = () => {
    if (authenticated && !isCreator) {
      submit();
    }
    setCurrentQuestion(undefined);
    setShowScore(true);
  };

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
      {currentQuestion && (
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
          <Box className="quiz-play-content">
            <Typography variant="h4" className="quiz-play-question">
              {currentQuestionNo + 1}. {currentQuestion.questionTitle}?
            </Typography>
            <List className="quiz-play-answers">
              <ListItemText
                onClick={() => {
                  !currentAnswer
                    ? handleAnswer(currentQuestion.firstAnswer)
                    : "";
                }}
                className={`quiz-play-answer${
                  currentAnswer === currentQuestion.firstAnswer
                    ? currentAnswer === currentQuestion.correctAnswer
                      ? "-correct"
                      : "-uncorrect"
                    : currentAnswer &&
                      currentQuestion.firstAnswer ===
                        currentQuestion.correctAnswer
                    ? "-correct"
                    : ""
                }`}
              >
                {currentQuestion.firstAnswer}
              </ListItemText>
              <ListItemText
                onClick={() => {
                  !currentAnswer
                    ? handleAnswer(currentQuestion.secondAnswer)
                    : "";
                }}
                className={`quiz-play-answer${
                  currentAnswer === currentQuestion.secondAnswer
                    ? currentAnswer === currentQuestion.correctAnswer
                      ? "-correct"
                      : "-uncorrect"
                    : currentAnswer &&
                      currentQuestion.secondAnswer ===
                        currentQuestion.correctAnswer
                    ? "-correct"
                    : ""
                }`}
              >
                {currentQuestion.secondAnswer}
              </ListItemText>
              <ListItemText
                onClick={() => {
                  !currentAnswer
                    ? handleAnswer(currentQuestion.thirdAnswer)
                    : "";
                }}
                className={`quiz-play-answer${
                  currentAnswer === currentQuestion.thirdAnswer
                    ? currentAnswer === currentQuestion.correctAnswer
                      ? "-correct"
                      : "-uncorrect"
                    : currentAnswer &&
                      currentQuestion.thirdAnswer ===
                        currentQuestion.correctAnswer
                    ? "-correct"
                    : ""
                }`}
              >
                {currentQuestion.thirdAnswer}
              </ListItemText>
              <ListItemText
                onClick={() => {
                  !currentAnswer
                    ? handleAnswer(currentQuestion.fourthAnswer)
                    : "";
                }}
                className={`quiz-play-answer${
                  currentAnswer === currentQuestion.fourthAnswer
                    ? currentAnswer === currentQuestion.correctAnswer
                      ? "-correct"
                      : "-uncorrect"
                    : currentAnswer &&
                      currentQuestion.fourthAnswer ===
                        currentQuestion.correctAnswer
                    ? "-correct"
                    : ""
                }`}
              >
                {currentQuestion.fourthAnswer}
              </ListItemText>
              <Box
                sx={{
                  position: "absolute",
                  bottom: -70,
                  left: 5,
                  cursor: "pointer",
                }}
              >
                <img width={30} height={30} src="/hint-icon.png"></img>
              </Box>
            </List>
            {}
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
      {quiz && quiz.questions.length === 0 && (
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
              {submitStatus === "saved" && lastGame ? lastGame.score : score} /{" "}
              {totalQuestionNo}
            </Typography>
            <Typography component="div" sx={{ marginTop: 3 }}>
              {resultNote()}
            </Typography>
            {submitStatus === "failed" && (
              <Button sx={{ marginTop: 1 }} onClick={() => submit()}>
                Pokušaj ponovno
              </Button>
            )}
          </Box>
          <Box className="quiz-play-footer">
            <Typography>
              {currentQuestionNo + 1}. od {totalQuestionNo}
            </Typography>
            <Button onClick={() => navigate("/")} variant="contained">
              Vrati na početnu
            </Button>
          </Box>
        </Paper>
      )}
    </Box>
  );
}
