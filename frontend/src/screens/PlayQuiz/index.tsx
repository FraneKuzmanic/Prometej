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
  // which of the four answers was clicked, 1 to 4; undefined until the Question is answered
  const [chosenOption, setChosenOption] = useState<number>();
  // Opening the Hint is the Student's choice; it does not change the Score.
  const [hintShown, setHintShown] = useState(false);
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
    setChosenOption(undefined);
    setHintShown(false);
  };

  const handleAnswer = (option: number) => {
    if (currentQuestion) {
      const isCorrect = option === currentQuestion.correctOption;
      setScore(score + (isCorrect ? 1 : 0));
      setChosenOption(option);
      setQuestionAnswered(true);
      setQuizAnswers([
        ...quizAnswers,
        {
          questionId: currentQuestion.id,
          chosenOption: option,
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
              {currentQuestionNo + 1}. {currentQuestion.questionTitle}
            </Typography>
            <List className="quiz-play-answers">
              {[
                currentQuestion.firstAnswer,
                currentQuestion.secondAnswer,
                currentQuestion.thirdAnswer,
                currentQuestion.fourthAnswer,
              ].map((text, index) => {
                const option = index + 1;
                // Once answered, the correct option turns green and a wrong choice red.
                const mark =
                  chosenOption === undefined
                    ? ""
                    : option === currentQuestion.correctOption
                    ? "-correct"
                    : option === chosenOption
                    ? "-uncorrect"
                    : "";
                return (
                  <ListItemText
                    key={option}
                    onClick={() => {
                      if (chosenOption === undefined) handleAnswer(option);
                    }}
                    className={`quiz-play-answer${mark}`}
                  >
                    {text}
                  </ListItemText>
                );
              })}
            </List>
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
            {chosenOption !== undefined && currentQuestion.exploreMore && (
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
