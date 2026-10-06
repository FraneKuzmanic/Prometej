import {
  Alert,
  Box,
  Button,
  Checkbox,
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
import { useEffect, useRef, useState } from "react";
import "./styles.css";
import QuestionContainer from "../QuestionContainer";
import SourceTextFields from "../QuestionContainer/SourceTextFields";
import {
  QuestionType,
  QuizCreateRequest,
  SourceTextRequest,
} from "../../types/models/Quiz";
import { fromLocalInput, toLocalInput } from "./closesAt";
import {
  EditorSourceTexts,
  EditorQuestion,
  isComplete,
  isSourceTextComplete,
  newQuestion,
} from "./questions";
import AddIcon from "@mui/icons-material/Add";
import MoreVertIcon from "@mui/icons-material/MoreVert";
import SaveIcon from "@mui/icons-material/Save";
import CancelIcon from "@mui/icons-material/Cancel";
import { useSelector } from "react-redux";
import { RootState, useAppDispatch } from "../../store/store";
import { fetchPeriods } from "../../store/slices/periodSlice";

interface QuizEditorProps {
  initialTitle: string;
  initialIsPrivate: boolean;
  initialPeriodId: number | null;
  initialIsTest: boolean;
  initialTimeLimitMinutes: number | null;
  initialClosesAt: string | null;
  initialQuestions: EditorQuestion[];
  initialSourceTexts: EditorSourceTexts;
  // Resolves to whether the Quiz was saved; the editor stays open when it was not.
  onSave: (
    quiz: QuizCreateRequest,
    questions: EditorQuestion[],
    sourceTexts: EditorSourceTexts
  ) => Promise<boolean>;
  onCancel: () => void;
}

// The server's limit of Questions for one Source Text.
const MAX_SOURCE_TEXT_QUESTIONS = 10;

// The server's limits for a time limit, in minutes.
const MIN_TIME_LIMIT = 1;
const MAX_TIME_LIMIT = 300;

// What a Question of each type still needs, said after "Pitanje {n} nije potpuno: ".
const missing: Record<QuestionType, string> = {
  choice: "unesite pitanje, četiri različita odgovora i označite točan.",
  matching: "unesite pitanje i tri do pet različitih parova.",
  ordering: "unesite pitanje i tri do šest različitih pojmova.",
};

export default function QuizEditor({
  initialTitle,
  initialIsPrivate,
  initialPeriodId,
  initialIsTest,
  initialTimeLimitMinutes,
  initialClosesAt,
  initialQuestions,
  initialSourceTexts,
  onSave,
  onCancel,
}: QuizEditorProps) {
  const dispatch = useAppDispatch();
  const { periods } = useSelector((state: RootState) => state.period);
  const [periodId, setPeriodId] = useState<number | null>(initialPeriodId);

  useEffect(() => {
    if (!periods) dispatch(fetchPeriods());
  }, [dispatch, periods]);

  // A new Quiz, and an old one stored without Questions, start with one to fill in.
  const [quizQuestions, setQuizQuestions] = useState<EditorQuestion[]>(
    initialQuestions.length > 0 ? initialQuestions : [newQuestion("choice")]
  );
  // The Source Texts by key; a Question names the key of the one it is asked about.
  const [sourceTexts, setSourceTexts] = useState<EditorSourceTexts>(initialSourceTexts);
  const newSourceTextNo = useRef(0);
  const [selected, setSelected] = useState<number>(0);
  const [quizTitle, setQuizTitle] = useState<string>(initialTitle);
  const [isPrivate, setIsPrivate] = useState<boolean>(initialIsPrivate);
  const [isTest, setIsTest] = useState<boolean>(initialIsTest);
  // as typed; empty for no limit
  const [timeLimit, setTimeLimit] = useState<string>(
    initialTimeLimitMinutes === null ? "" : String(initialTimeLimitMinutes)
  );
  // the value of a datetime-local field, in the Teacher's own time; empty for none
  const [closesAt, setClosesAt] = useState<string>(
    initialClosesAt === null ? "" : toLocalInput(initialClosesAt)
  );
  const [isSaving, setIsSaving] = useState<boolean>(false);
  const [saveFailed, setSaveFailed] = useState<boolean>(false);
  const [inputDrawer, setInputDrawer] = useState<boolean>(false);
  // The first Question that kept the Quiz from being saved, its type, and whether it was
  // its Source Text that is not filled in.
  const [incomplete, setIncomplete] = useState<{
    no: number;
    type: QuestionType;
    sourceText: boolean;
  } | null>(null);
  // One menu for the whole strip; it remembers which Question it was opened for.
  const [menu, setMenu] = useState<{
    anchor: HTMLElement;
    index: number;
  } | null>(null);
  const [addMenuAnchor, setAddMenuAnchor] = useState<HTMLElement | null>(null);

  const sourceTextOf = (question: EditorQuestion) =>
    question.sourceTextKey ? sourceTexts[question.sourceTextKey] : undefined;

  const handleSave = () => {
    const index = quizQuestions.findIndex((question) => {
      const sourceText = sourceTextOf(question);
      return !isComplete(question) || (sourceText && !isSourceTextComplete(sourceText));
    });
    if (index !== -1) {
      const sourceText = sourceTextOf(quizQuestions[index]);
      setSelected(index);
      setIncomplete({
        no: index + 1,
        type: quizQuestions[index].type,
        sourceText: !!sourceText && !isSourceTextComplete(sourceText),
      });
      return;
    }
    setSaveFailed(false);
    setInputDrawer(true);
  };

  // A time limit means something where the Quiz can be sat: a Test, and a Public Quiz
  // solved as one. A Private Quiz that is practice has none.
  const hasTimeLimit = isTest || !isPrivate;
  const timeLimitMinutes = hasTimeLimit && timeLimit !== "" ? Number(timeLimit) : null;
  const timeLimitValid =
    timeLimitMinutes === null ||
    (Number.isInteger(timeLimitMinutes) &&
      timeLimitMinutes >= MIN_TIME_LIMIT &&
      timeLimitMinutes <= MAX_TIME_LIMIT);

  const saveQuiz = () => {
    setIsSaving(true);
    setSaveFailed(false);
    onSave(
      {
        title: quizTitle.trim(),
        isPrivate,
        periodId,
        isTest,
        timeLimitMinutes,
        closesAt: isTest && closesAt !== "" ? fromLocalInput(closesAt) : null,
      },
      quizQuestions,
      sourceTexts
    ).then((saved) => {
      setIsSaving(false);
      setSaveFailed(!saved);
    });
  };

  const insertQuestion = (index: number, question: EditorQuestion) => {
    setSelected(index);
    setQuizQuestions([
      ...quizQuestions.slice(0, index),
      question,
      ...quizQuestions.slice(index),
    ]);
    setIncomplete(null);
    setAddMenuAnchor(null);
  };

  // The type is chosen here and stays: the fields of one type say nothing in another.
  const addQuestion = (type: QuestionType) =>
    insertQuestion(quizQuestions.length, newQuestion(type));

  // A new Source Text comes with its first Question.
  const addSourceText = () => {
    const sourceTextKey = `new-${newSourceTextNo.current++}`;
    setSourceTexts({ ...sourceTexts, [sourceTextKey]: { id: 0, caption: "", body: "" } });
    insertQuestion(quizQuestions.length, { ...newQuestion("choice"), sourceTextKey });
  };

  // The Questions of one Source Text stay together: a new one goes after the last of them.
  const addToSourceText = (sourceTextKey: string) => {
    const last = quizQuestions.reduce(
      (found, question, index) =>
        question.sourceTextKey === sourceTextKey ? index : found,
      -1
    );
    insertQuestion(last + 1, { ...newQuestion("choice"), sourceTextKey });
  };

  const updateSourceText = (
    sourceTextKey: string,
    updates: Partial<SourceTextRequest>
  ) => {
    setSourceTexts((prevSourceTexts) => ({
      ...prevSourceTexts,
      [sourceTextKey]: { ...prevSourceTexts[sourceTextKey], ...updates },
    }));
    setIncomplete(null);
  };

  const handleDelete = (index: number) => {
    const newQuestions = quizQuestions.filter((_, i) => i !== index);
    setSelected(
      selected > index ? selected - 1 : Math.min(selected, newQuestions.length - 1)
    );
    setQuizQuestions(newQuestions);
    // A Source Text goes with its last Question.
    const { sourceTextKey } = quizQuestions[index];
    if (
      sourceTextKey &&
      !newQuestions.some((question) => question.sourceTextKey === sourceTextKey)
    ) {
      setSourceTexts((prevSourceTexts) => {
        const rest = { ...prevSourceTexts };
        delete rest[sourceTextKey];
        return rest;
      });
    }
    setIncomplete(null);
    setMenu(null);
  };

  // A Question can be added in the middle of the strip, so the strip follows the selected one.
  useEffect(() => {
    const container = document.querySelector(".questions-nav");
    if (!container) return;
    if (selected === quizQuestions.length - 1) {
      container.scrollLeft = container.scrollWidth;
    } else {
      container
        .querySelector(".question-nav-container.selected")
        ?.scrollIntoView({ block: "nearest", inline: "nearest" });
    }
  }, [quizQuestions.length, selected]);

  // "Tekst 1", "Tekst 2": the Source Texts numbered in the order the Questions use them.
  const sourceTextNumbers = new Map<string, number>();
  quizQuestions.forEach(({ sourceTextKey }) => {
    if (sourceTextKey && !sourceTextNumbers.has(sourceTextKey)) {
      sourceTextNumbers.set(sourceTextKey, sourceTextNumbers.size + 1);
    }
  });
  const selectedSourceTextKey = quizQuestions[selected]?.sourceTextKey;

  const updateQuestion = (
    questionNo: number,
    updates: Partial<EditorQuestion>
  ) => {
    setQuizQuestions((prevQuestions) =>
      prevQuestions.map((question, index) =>
        index === questionNo ? { ...question, ...updates } : question
      )
    );
    setIncomplete(null);
  };

  return (
    <Box className="quiz-screen-wrapper">
      {incomplete !== null && (
        <Alert severity="warning" className="quiz-editor-message">
          {incomplete.sourceText
            ? `Polazni tekst uz pitanje ${incomplete.no} nije potpun: unesite autora i naslov te tekst.`
            : `Pitanje ${incomplete.no} nije potpuno: ${missing[incomplete.type]}`}
        </Alert>
      )}
      <QuestionContainer
        currentQuestion={quizQuestions[selected]}
        selected={selected}
        handleQuestionChange={updateQuestion}
        sourceTextFields={
          selectedSourceTextKey && (
            <SourceTextFields
              sourceText={sourceTexts[selectedSourceTextKey]}
              onChange={(updates) => updateSourceText(selectedSourceTextKey, updates)}
              onAddQuestion={
                quizQuestions.filter(
                  (question) => question.sourceTextKey === selectedSourceTextKey
                ).length < MAX_SOURCE_TEXT_QUESTIONS
                  ? () => addToSourceText(selectedSourceTextKey)
                  : undefined
              }
            />
          )
        }
      />
      <Box className="questions-nav">
        {quizQuestions.map((question, index) => (
          <Paper
            className={`question-nav-container ${
              selected === index ? "selected" : ""
            } ${question.sourceTextKey ? "with-source-text" : ""}`}
            onClick={() => setSelected(index)}
            key={index}
          >
            {question.sourceTextKey && (
              <Typography className="question-container-source-text">
                Tekst {sourceTextNumbers.get(question.sourceTextKey)}
              </Typography>
            )}
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
        <Paper
          onClick={(event) => setAddMenuAnchor(event.currentTarget)}
          className="question-container-add"
        >
          <AddIcon />
        </Paper>
        <Menu
          anchorEl={addMenuAnchor}
          open={addMenuAnchor !== null}
          onClose={() => setAddMenuAnchor(null)}
        >
          <MenuItem onClick={() => addQuestion("choice")}>
            Pitanje s četiri odgovora
          </MenuItem>
          <MenuItem onClick={() => addQuestion("matching")}>Povezivanje</MenuItem>
          <MenuItem onClick={() => addQuestion("ordering")}>Redanje</MenuItem>
          <MenuItem onClick={() => addSourceText()}>
            Polazni tekst s pitanjima
          </MenuItem>
        </Menu>
        <SpeedDial
          ariaLabel="Radnje kviza"
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
          <TextField
            select
            fullWidth
            size="small"
            label="Razdoblje"
            sx={{ marginTop: "1rem" }}
            // The chosen Period is kept while the list loads; the field only cannot show it yet.
            disabled={!periods}
            value={periods && periodId !== null ? String(periodId) : ""}
            onChange={(e) =>
              setPeriodId(e.target.value ? Number(e.target.value) : null)
            }
            SelectProps={{ displayEmpty: true }}
            InputLabelProps={{ shrink: true }}
          >
            <MenuItem value="">Bez razdoblja</MenuItem>
            {periods?.map((period) => (
              <MenuItem key={period.id} value={String(period.id)}>
                {period.name}
              </MenuItem>
            ))}
          </TextField>
          <FormControlLabel
            sx={{ marginTop: "0.5rem" }}
            control={
              <Switch
                checked={isPrivate}
                onChange={(e) => {
                  setIsPrivate(e.target.checked);
                  // Only a Private Quiz can be a Test.
                  if (!e.target.checked) setIsTest(false);
                }}
              />
            }
            label="Privatni kviz"
          />
          <FormControlLabel
            control={
              <Checkbox
                checked={isTest}
                disabled={!isPrivate}
                onChange={(e) => setIsTest(e.target.checked)}
              />
            }
            label="Provjera (jedan pokušaj, bez povratne informacije)"
          />
          {hasTimeLimit && (
            <TextField
              type="number"
              fullWidth
              size="small"
              label="Vremensko ograničenje (min)"
              sx={{ marginTop: "1rem" }}
              inputProps={{ min: MIN_TIME_LIMIT, max: MAX_TIME_LIMIT }}
              InputLabelProps={{ shrink: true }}
              value={timeLimit}
              onChange={(e) => setTimeLimit(e.target.value)}
              error={!timeLimitValid}
              helperText={
                timeLimitValid
                  ? "Prazno: bez ograničenja"
                  : `Cijeli broj od ${MIN_TIME_LIMIT} do ${MAX_TIME_LIMIT}`
              }
            />
          )}
          {isTest && (
            <TextField
              type="datetime-local"
              fullWidth
              size="small"
              label="Zatvara se"
              sx={{ marginTop: "1rem" }}
              InputLabelProps={{ shrink: true }}
              value={closesAt}
              onChange={(e) => setClosesAt(e.target.value)}
              helperText="Prazno: dok je ne zatvorite"
            />
          )}
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
              quizTitle.trim() && timeLimitValid ? saveQuiz() : null;
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
