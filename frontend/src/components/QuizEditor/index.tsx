import {
  Alert,
  Box,
  Button,
  FormControlLabel,
  IconButton,
  LinearProgress,
  Menu,
  MenuItem,
  Paper,
  SpeedDial,
  SpeedDialAction,
  SpeedDialIcon,
  Switch,
  TextField,
  Tooltip,
  Typography,
} from "@mui/material";
import { useEffect, useState } from "react";
import "./styles.css";
import QuestionContainer from "../QuestionContainer";
import { QuestionCreateRequest } from "../../types/models/Quiz";
import AddIcon from "@mui/icons-material/Add";
import MoreVertIcon from "@mui/icons-material/MoreVert";
import SaveIcon from "@mui/icons-material/Save";
import CancelIcon from "@mui/icons-material/Cancel";

// A Question already stored has an id; one added in the editor has none yet.
export type EditorQuestion = QuestionCreateRequest & { id?: number };

interface QuizEditorProps {
  initialTitle: string;
  initialIsPrivate: boolean;
  initialQuestions: EditorQuestion[];
  // Resolves to whether the Quiz was saved; the editor stays open when it was not.
  onSave: (
    title: string,
    isPrivate: boolean,
    questions: EditorQuestion[]
  ) => Promise<boolean>;
  onCancel: () => void;
}

const emptyQuestion: EditorQuestion = {
  questionTitle: "",
  firstAnswer: "",
  secondAnswer: "",
  thirdAnswer: "",
  fourthAnswer: "",
  correctOption: 0,
  hintText: "",
  exploreMore: "",
};

// The server's rule for a Question, checked here first so the Teacher is told which one.
const isComplete = (question: EditorQuestion) => {
  const options = [
    question.firstAnswer,
    question.secondAnswer,
    question.thirdAnswer,
    question.fourthAnswer,
  ].map((option) => option.trim());
  return (
    question.questionTitle.trim() !== "" &&
    options.every((option) => option !== "") &&
    new Set(options).size === options.length &&
    question.correctOption >= 1 &&
    question.correctOption <= 4
  );
};

export default function QuizEditor({
  initialTitle,
  initialIsPrivate,
  initialQuestions,
  onSave,
  onCancel,
}: QuizEditorProps) {
  // A new Quiz, and an old one stored without Questions, start with one to fill in.
  const [quizQuestions, setQuizQuestions] = useState<EditorQuestion[]>(
    initialQuestions.length > 0 ? initialQuestions : [emptyQuestion]
  );
  const [selected, setSelected] = useState<number>(0);
  const [quizTitle, setQuizTitle] = useState<string>(initialTitle);
  const [isPrivate, setIsPrivate] = useState<boolean>(initialIsPrivate);
  const [isSaving, setIsSaving] = useState<boolean>(false);
  const [saveFailed, setSaveFailed] = useState<boolean>(false);
  const [inputDrawer, setInputDrawer] = useState<boolean>(false);
  // The number of the first Question that kept the Quiz from being saved.
  const [incompleteNo, setIncompleteNo] = useState<number | null>(null);
  // One menu for the whole strip; it remembers which Question it was opened for.
  const [menu, setMenu] = useState<{
    anchor: HTMLElement;
    index: number;
  } | null>(null);

  const handleSave = () => {
    const incomplete = quizQuestions.findIndex(
      (question) => !isComplete(question)
    );
    if (incomplete !== -1) {
      setSelected(incomplete);
      setIncompleteNo(incomplete + 1);
      return;
    }
    setSaveFailed(false);
    setInputDrawer(true);
  };

  const saveQuiz = () => {
    setIsSaving(true);
    setSaveFailed(false);
    onSave(quizTitle.trim(), isPrivate, quizQuestions).then((saved) => {
      setIsSaving(false);
      setSaveFailed(!saved);
    });
  };

  const addQuestion = () => {
    setSelected(quizQuestions.length);
    setQuizQuestions([...quizQuestions, emptyQuestion]);
    setIncompleteNo(null);
  };

  const handleDelete = (index: number) => {
    const newQuestions = quizQuestions.filter((_, i) => i !== index);
    setSelected(
      selected > index ? selected - 1 : Math.min(selected, newQuestions.length - 1)
    );
    setQuizQuestions(newQuestions);
    setIncompleteNo(null);
    setMenu(null);
  };

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
    setIncompleteNo(null);
  };

  return (
    <Box className="quiz-screen-wrapper">
      {incompleteNo !== null && (
        <Alert severity="warning" className="quiz-editor-message">
          Pitanje {incompleteNo} nije potpuno: unesite pitanje, četiri različita
          odgovora i označite točan.
        </Alert>
      )}
      <QuestionContainer
        currentQuestion={quizQuestions[selected]}
        selected={selected}
        handleQuestionChange={updateQuestion}
      />
      <Box className="questions-nav">
        {quizQuestions.map((question, index) => (
          <Paper
            className={`question-nav-container ${
              selected === index ? "selected" : ""
            }`}
            onClick={() => setSelected(index)}
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
                  aria-label={`Mogućnosti pitanja ${index + 1}`}
                  aria-haspopup="true"
                  onClick={(event) =>
                    setMenu({ anchor: event.currentTarget, index })
                  }
                >
                  <MoreVertIcon />
                </IconButton>
              </Box>
            )}
          </Paper>
        ))}
        <Menu
          anchorEl={menu?.anchor}
          open={menu !== null}
          onClose={() => setMenu(null)}
        >
          <MenuItem onClick={() => menu && handleDelete(menu.index)}>
            Izbriši
          </MenuItem>
        </Menu>
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
            onClick={() => handleSave()}
          />
          <SpeedDialAction
            key={"Odustani"}
            icon={<CancelIcon />}
            tooltipTitle={"Odustani"}
            onClick={() => onCancel()}
          />
        </SpeedDial>
      </Box>
      {inputDrawer && (
        <Paper elevation={24} className="title-input">
          <Typography sx={{ marginTop: "1rem" }} variant="h5">
            Unesite naslov kviza
          </Typography>
          <TextField
            sx={{ marginTop: "2rem" }}
            type="text"
            placeholder="Naslov kviza"
            fullWidth
            required
            inputProps={{ maxLength: 100 }}
            value={quizTitle}
            onChange={(e) => setQuizTitle(e.target.value)}
          />
          <FormControlLabel
            sx={{ marginTop: "0.5rem" }}
            control={
              <Switch
                checked={isPrivate}
                onChange={(e) => setIsPrivate(e.target.checked)}
              />
            }
            label="Privatni kviz"
          />
          {isSaving ? <LinearProgress sx={{ width: "100%" }} /> : null}
          {saveFailed && (
            <Typography color="error" sx={{ fontSize: 14 }}>
              Kviz nije spremljen. Pokušajte ponovno.
            </Typography>
          )}
          <Button
            variant="contained"
            disabled={isSaving}
            onClick={() => {
              quizTitle.trim() ? saveQuiz() : null;
            }}
            style={{
              backgroundColor: "#553b08",
              marginTop: "1rem",
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
