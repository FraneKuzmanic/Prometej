import { useEffect, useState } from "react";
import {
  AnswerCreateRequest,
  QuestionViewModel,
  QuizGameCreateRequest,
} from "../../types/models/Quiz";
import { useSelector } from "react-redux";
import { RootState, useAppDispatch } from "../../store/store";
import { useNavigate, useParams } from "react-router-dom";
import { fetchQuiz, submitQuiz } from "../../store/slices/quizSlice";
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
  const { quiz } = useSelector((state: RootState) => state.quiz);
  const { user } = useSelector((state: RootState) => state.user);
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

  useEffect(() => {
    dispatch(fetchQuiz(Number(id)));
  }, []);

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
          correctAnswer: currentQuestion.correctAnswer,
        },
      ]);
    }
  };

  const handleShowScore = () => {
    const quizGame: QuizGameCreateRequest = {
      quizId: quiz ? quiz.id : 0,
      userId: user ? user.id : undefined,
      userName: user ? user.firstName + " " + user.lastName : "",
      score,
      datePlayed: new Date(),
    };
    dispatch(submitQuiz({ quizGame: quizGame, answers: quizAnswers }));
    setCurrentQuestion(undefined);
    setShowScore(true);
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
              {score} / {totalQuestionNo}
            </Typography>
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
