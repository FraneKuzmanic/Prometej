import {
  Box,
  Button,
  IconButton,
  LinearProgress,
  Menu,
  MenuItem,
  Paper,
  SpeedDial,
  SpeedDialAction,
  SpeedDialIcon,
  TextField,
  Tooltip,
  Typography,
} from "@mui/material";
import { useEffect, useState } from "react";
import "./styles.css";
import QuestionContainer from "../../components/QuestionContainer";
import {
  QuestionCreateRequest,
  QuizCreateRequest,
} from "../../types/models/Quiz";
import AddIcon from "@mui/icons-material/Add";
import MoreVertIcon from "@mui/icons-material/MoreVert";
import SaveIcon from "@mui/icons-material/Save";
import CancelIcon from "@mui/icons-material/Cancel";
import React from "react";
import { createQuiz } from "../../store/slices/quizSlice";
import { useSelector } from "react-redux";
import { RootState, useAppDispatch } from "../../store/store";
import { useNavigate } from "react-router-dom";

export default function MakeQuiz() {
  const [quizQuestions, setQuizQuestions] = useState<QuestionCreateRequest[]>(
    []
  );
  const [selected, setSelected] = useState<number>(0);
  const [currentQuestion, setCurrentQuestion] = useState<QuestionCreateRequest>(
    {
      questionTitle: "",
      firstAnswer: "",
      secondAnswer: "",
      thirdAnswer: "",
      fourthAnswer: "",
      correctAnswer: "",
      hintText: "",
      exploreMore: "",
    }
  );
  const [quizTitle, setQuizTitle] = useState<string>("");
  const [isSaving, setIsSaving] = useState<boolean>(false);
  const [inputDrawer, setInputDrawer] = useState<boolean>(false);
  const { user } = useSelector((state: RootState) => state.user);
  const dispatch = useAppDispatch();
  const navigate = useNavigate();
  const [anchorEl, setAnchorEl] = React.useState<null | HTMLElement>(null);
  const open = Boolean(anchorEl);
  const handleClick = (event: React.MouseEvent<HTMLButtonElement>) => {
    setAnchorEl(event.currentTarget);
  };
  const handleClose = () => {
    setAnchorEl(null);
  };

  const handleCancel = () => {
    navigate("/learning");
  };

  const generateRandomNumber = () => {
    return Math.floor(10000 + Math.random() * 90000);
  };

  const saveQuiz = () => {
    const entryCode = generateRandomNumber();
    const quiz: QuizCreateRequest = {
      title: quizTitle,
      isPrivate: true,
      creatorId: user ? user.id : 0,
      entryCode: entryCode,
    };
    const questions: QuestionCreateRequest[] = [...quizQuestions];
    setIsSaving(true);
    dispatch(createQuiz({ quiz: quiz, questions: questions })).then(() => {
      setIsSaving(false);
      setInputDrawer(false);
      navigate("/learning");
    });
  };

  const addQuestion = () => {
    const newQuestion: QuestionCreateRequest = {
      questionTitle: "",
      firstAnswer: "",
      secondAnswer: "",
      thirdAnswer: "",
      fourthAnswer: "",
      correctAnswer: "",
      hintText: "",
      exploreMore: "",
    };
    setSelected(quizQuestions.length);
    setCurrentQuestion(newQuestion);
    setQuizQuestions([...quizQuestions, newQuestion]);
  };

  const handleDelete = () => {
    console.log(selected);
    const newIndex = selected > 0 ? selected - 1 : selected;
    const newQuestions = [...quizQuestions];
    newQuestions.splice(selected, 1);
    const newCurrentQuestion =
      selected > 0 ? newQuestions[selected - 1] : newQuestions[selected];
    setCurrentQuestion(newCurrentQuestion);
    setSelected(newIndex);
    setQuizQuestions(newQuestions);
    handleClose();
  };

  useEffect(() => {
    setQuizQuestions([...quizQuestions, currentQuestion]);
  }, []);

  useEffect(() => {
    const container = document.querySelector(".questions-nav");
    if (container) {
      container.scrollLeft = container.scrollWidth;
    }
  }, [quizQuestions]);

  const updateQuestion = (
    questionNo: number,
    updates: Partial<QuestionCreateRequest>
  ) => {
    setQuizQuestions((prevQuestions) =>
      prevQuestions.map((question, index) =>
        index === questionNo ? { ...question, ...updates } : question
      )
    );
    setCurrentQuestion((prevQuestion) => ({ ...prevQuestion, ...updates }));
  };

  return (
    <Box className="quiz-screen-wrapper">
      <QuestionContainer
        currentQuestion={currentQuestion}
        selected={selected}
        handleQuestionChange={updateQuestion}
      />
      <Box className="questions-nav">
        {quizQuestions.map((question: QuestionCreateRequest, index) => (
          <Paper
            className={`question-nav-container ${
              selected === index ? "selected" : ""
            }`}
            onClick={() => {
              setSelected(index);
              setCurrentQuestion(question);
            }}
            key={index}
          >
            <Tooltip
              title={
                <Typography sx={{ fontSize: 14 }}>
                  {question.questionTitle}
                </Typography>
              }
              placement="top"
            >
              <Typography className="question-container-title">
                {question.questionTitle}
              </Typography>
            </Tooltip>
            <Typography className="question-container-no">
              {index + 1}.
            </Typography>
            {quizQuestions.length > 1 && (
              <Box className="question-container-opt">
                <IconButton
                  aria-label="more"
                  id="long-button"
                  aria-controls={open ? "basic-menu" : undefined}
                  aria-expanded={open ? "true" : undefined}
                  aria-haspopup="true"
                  onClick={handleClick}
                >
                  <MoreVertIcon />
                </IconButton>
                <Menu
                  id="basic-menu"
                  anchorEl={anchorEl}
                  open={open}
                  onClose={handleClose}
                  MenuListProps={{
                    "aria-labelledby": "basic-button",
                  }}
                >
                  <MenuItem
                    onClick={(event) => {
                      event.stopPropagation();
                      handleDelete();
                    }}
                  >
                    Izbriši
                  </MenuItem>
                </Menu>
              </Box>
            )}
          </Paper>
        ))}
        <Paper onClick={() => addQuestion()} className="question-container-add">
          <AddIcon />
        </Paper>
        <SpeedDial
          ariaLabel="SpeedDial basic example"
          sx={{
            position: "fixed",
            bottom: 16,
            right: 16,
          }}
          className="speed-dial"
          icon={<SpeedDialIcon />}
        >
          <SpeedDialAction
            key={"Spremi"}
            icon={<SaveIcon />}
            tooltipTitle={"Spremi"}
            onClick={() => setInputDrawer(true)}
          />
          <SpeedDialAction
            key={"Odustani"}
            icon={<CancelIcon />}
            tooltipTitle={"Odustani"}
            onClick={() => handleCancel()}
          />
        </SpeedDial>
      </Box>
      {inputDrawer && (
        <Paper elevation={24} className="title-input">
          <Typography sx={{ marginTop: "1rem" }} variant="h5">
            Unesite naslov kviza
          </Typography>
          <TextField
            sx={{ marginTop: "3.5rem" }}
            type="text"
            placeholder="Naslov kviza"
            fullWidth
            required
            value={quizTitle}
            onChange={(e) => setQuizTitle(e.target.value)}
          />
          {isSaving ? <LinearProgress /> : null}
          <Button
            variant="contained"
            onClick={() => {
              quizTitle ? saveQuiz() : null;
            }}
            style={{
              backgroundColor: "#553b08",
              marginTop: "2rem",
              width: "80%",
            }}
          >
            Spremi
          </Button>
          <Button
            variant="contained"
            style={{
              backgroundColor: "#553b08",
              marginTop: "1rem",
              width: "80%",
            }}
            onClick={() => setInputDrawer(false)}
          >
            Odustani
          </Button>
        </Paper>
      )}
    </Box>
  );
}
