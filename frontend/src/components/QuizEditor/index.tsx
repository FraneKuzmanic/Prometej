import {
  Alert,
  Box,
  Button,
  ButtonBase,
  Checkbox,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Divider,
  FormControlLabel,
  IconButton,
  LinearProgress,
  ListItemIcon,
  ListItemText,
  Menu,
  MenuItem,
  Switch,
  TextField,
  ToggleButton,
  ToggleButtonGroup,
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
import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import ArticleOutlinedIcon from "@mui/icons-material/ArticleOutlined";
import AutoAwesomeOutlinedIcon from "@mui/icons-material/AutoAwesomeOutlined";
import DeleteOutlineIcon from "@mui/icons-material/DeleteOutline";
import FormatListNumberedIcon from "@mui/icons-material/FormatListNumbered";
import MoreVertIcon from "@mui/icons-material/MoreVert";
import RadioButtonCheckedIcon from "@mui/icons-material/RadioButtonChecked";
import SyncAltIcon from "@mui/icons-material/SyncAlt";
import { useSelector } from "react-redux";
import { RootState, useAppDispatch } from "../../store/store";
import { fetchPeriods } from "../../store/slices/periodSlice";
import { fetchTutorStatus } from "../../store/slices/tutorSlice";
import SuggestQuestions from "./SuggestQuestions";

interface QuizEditorProps {
  initialTitle: string;
  initialIsPrivate: boolean;
  initialPeriodId: number | null;
  initialIsTest: boolean;
  initialTimeLimitMinutes: number | null;
  initialClosesAt: string | null;
  initialQuestions: EditorQuestion[];
  initialSourceTexts: EditorSourceTexts;
  // A started Test: its Questions are shown and cannot be changed; its header can.
  locked?: boolean;
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

// The three kinds, as the editor names and describes them where one is chosen.
const kinds: {
  type: QuestionType;
  name: string;
  short: string;
  about: string;
  icon: JSX.Element;
}[] = [
  {
    type: "choice",
    name: "Pitanje s četiri odgovora",
    short: "Četiri odgovora",
    about: "Jedan točan odgovor od četiri ponuđena",
    icon: <RadioButtonCheckedIcon fontSize="small" />,
  },
  {
    type: "matching",
    name: "Povezivanje",
    short: "Povezivanje",
    about: "Tri do pet parova, bod za svaki točan par",
    icon: <SyncAltIcon fontSize="small" />,
  },
  {
    type: "ordering",
    name: "Redanje",
    short: "Redanje",
    about: "Tri do šest pojmova koje treba poredati",
    icon: <FormatListNumberedIcon fontSize="small" />,
  },
];
const kindOf = (type: QuestionType) => kinds.find((kind) => kind.type === type)!;

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
  locked = false,
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
  const [suggesting, setSuggesting] = useState<boolean>(false);
  const tutorAvailable = useSelector((state: RootState) => state.tutor.available);

  // The editor is outside the layout that asks this on the learning screens.
  useEffect(() => {
    if (tutorAvailable === undefined) dispatch(fetchTutorStatus());
  }, [dispatch, tutorAvailable]);

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

  // The tutor's drafts go to the end, as Questions like any other; the first is opened.
  const addDrafts = (drafts: EditorQuestion[]) => {
    setSelected(quizQuestions.length);
    setQuizQuestions([...quizQuestions, ...drafts]);
    setIncomplete(null);
    setSuggesting(false);
  };

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

  // A Question can be added in the middle of the list, so the list follows the selected one.
  useEffect(() => {
    document
      .querySelector(".questions-nav .question-nav-container.selected")
      ?.scrollIntoView({ block: "nearest", inline: "nearest" });
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

  // A Question that is not stored yet can still become another kind: its title and its two
  // explanations stay, the rest is the new kind's. One asked about a Source Text is a
  // choice Question, and a stored one keeps its type for good.
  const current = quizQuestions[selected];
  const kindIsOpen =
    !locked && current !== undefined && current.id === undefined && !current.sourceTextKey;
  const changeKind = (type: QuestionType) =>
    updateQuestion(selected, {
      ...newQuestion(type),
      questionTitle: current.questionTitle,
      hintText: current.hintText,
      exploreMore: current.exploreMore,
    });

  return (
    <Box className="quiz-screen-wrapper">
      <Box component="header" className="quiz-editor-bar">
        <Tooltip title="Natrag">
          <IconButton aria-label="Natrag" onClick={() => onCancel()}>
            <ArrowBackIcon />
          </IconButton>
        </Tooltip>
        <Typography variant="h6" component="h1" className="quiz-editor-name">
          {quizTitle.trim() || "Novi kviz"}
        </Typography>
        <Box className="quiz-editor-actions">
          <Button onClick={() => onCancel()}>Odustani</Button>
          <Button variant="contained" onClick={() => handleSave()}>
            Spremi
          </Button>
        </Box>
      </Box>
      <Box component="nav" className="questions-nav" aria-label="Pitanja">
        {quizQuestions.map((question, index) => (
          <Box
            className={`question-nav-container ${
              selected === index ? "selected" : ""
            } ${question.sourceTextKey ? "with-source-text" : ""}`}
            key={index}
          >
            <ButtonBase
              className="question-nav-open"
              aria-current={selected === index ? "true" : undefined}
              onClick={() => setSelected(index)}
            >
              <span className="question-container-no">{index + 1}.</span>
              <span className="question-container-text">
                <span className="question-container-title">
                  {question.questionTitle.trim() || "Bez teksta"}
                </span>
                <span className="question-container-kind">
                  {kindOf(question.type).short}
                  {question.sourceTextKey && (
                    <span className="question-container-source-text">
                      {" · "}Tekst {sourceTextNumbers.get(question.sourceTextKey)}
                    </span>
                  )}
                </span>
              </span>
            </ButtonBase>
            {quizQuestions.length > 1 && !locked && (
              <span className="question-container-opt">
                <IconButton
                  size="small"
                  aria-label={`Mogućnosti pitanja ${index + 1}`}
                  aria-haspopup="true"
                  onClick={(event) =>
                    setMenu({ anchor: event.currentTarget, index })
                  }
                >
                  <MoreVertIcon fontSize="small" />
                </IconButton>
              </span>
            )}
          </Box>
        ))}
        <Menu
          anchorEl={menu?.anchor}
          open={menu !== null}
          onClose={() => setMenu(null)}
        >
          <MenuItem
            className="menu-item-danger"
            onClick={() => menu && handleDelete(menu.index)}
          >
            <ListItemIcon>
              <DeleteOutlineIcon fontSize="small" />
            </ListItemIcon>
            Obriši
          </MenuItem>
        </Menu>
        {!locked && (
          <Button
            variant="outlined"
            startIcon={<AddIcon />}
            aria-haspopup="true"
            onClick={(event) => setAddMenuAnchor(event.currentTarget)}
            className="question-container-add"
          >
            Dodaj pitanje
          </Button>
        )}
        <Menu
          anchorEl={addMenuAnchor}
          open={addMenuAnchor !== null}
          onClose={() => setAddMenuAnchor(null)}
          className="quiz-editor-add-menu"
        >
          {kinds.map((kind) => (
            <MenuItem key={kind.type} onClick={() => addQuestion(kind.type)}>
              <ListItemIcon>{kind.icon}</ListItemIcon>
              <ListItemText primary={kind.name} secondary={kind.about} />
            </MenuItem>
          ))}
          <MenuItem onClick={() => addSourceText()}>
            <ListItemIcon>
              <ArticleOutlinedIcon fontSize="small" />
            </ListItemIcon>
            <ListItemText
              primary="Polazni tekst s pitanjima"
              secondary="Pjesma ili ulomak i do deset pitanja o njemu"
            />
          </MenuItem>
          {tutorAvailable && <Divider />}
          {tutorAvailable && (
            <MenuItem
              onClick={() => {
                setAddMenuAnchor(null);
                setSuggesting(true);
              }}
            >
              <ListItemIcon>
                <AutoAwesomeOutlinedIcon fontSize="small" />
              </ListItemIcon>
              <ListItemText
                primary="Predloži pitanja"
                secondary="Prometej ih sastavlja iz poglavlja gradiva"
              />
            </MenuItem>
          )}
        </Menu>
        {suggesting && (
          <SuggestQuestions
            initialPeriodId={periodId}
            onAdd={addDrafts}
            onClose={() => setSuggesting(false)}
          />
        )}
      </Box>
      <Box component="main" className="quiz-editor-main">
        {incomplete !== null && (
          <Alert severity="warning" className="quiz-editor-message">
            {incomplete.sourceText
              ? `Polazni tekst uz pitanje ${incomplete.no} nije potpun: unesite autora i naslov te tekst.`
              : `Pitanje ${incomplete.no} nije potpuno: ${missing[incomplete.type]}`}
          </Alert>
        )}
        {locked && (
          <Alert severity="info" className="quiz-editor-message">
            Provjera je započeta, pa se pitanja više ne mogu mijenjati. Naslov i
            vrijeme možete promijeniti.
          </Alert>
        )}
        {/* A disabled fieldset disables every field inside it. */}
        <fieldset disabled={locked} className="quiz-editor-questions">
          <QuestionContainer
            currentQuestion={current}
            selected={selected}
            handleQuestionChange={updateQuestion}
            kindField={
              current &&
              (kindIsOpen ? (
                <ToggleButtonGroup
                  exclusive
                  size="small"
                  color="primary"
                  aria-label="Vrsta pitanja"
                  value={current.type}
                  onChange={(_event, type: QuestionType | null) =>
                    type && changeKind(type)
                  }
                >
                  {kinds.map((kind) => (
                    <ToggleButton key={kind.type} value={kind.type}>
                      {kind.icon}
                      {kind.short}
                    </ToggleButton>
                  ))}
                </ToggleButtonGroup>
              ) : (
                <span className="question-kind-fixed">
                  {kindOf(current.type).icon}
                  {kindOf(current.type).name}
                </span>
              ))
            }
            sourceTextFields={
              selectedSourceTextKey && (
                <SourceTextFields
                  sourceText={sourceTexts[selectedSourceTextKey]}
                  number={sourceTextNumbers.get(selectedSourceTextKey)}
                  onChange={(updates) => updateSourceText(selectedSourceTextKey, updates)}
                  onAddQuestion={
                    !locked &&
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
        </fieldset>
      </Box>
      <Dialog
        open={inputDrawer}
        onClose={() => setInputDrawer(false)}
        maxWidth="xs"
        fullWidth
        PaperProps={{ className: "title-input" }}
      >
        <DialogTitle>Spremi kviz</DialogTitle>
        <DialogContent className="quiz-editor-save">
          <TextField
            sx={{ marginTop: 1 }}
            label="Naslov kviza"
            placeholder="Naslov kviza"
            autoFocus
            fullWidth
            required
            inputProps={{ maxLength: 100 }}
            InputLabelProps={{ shrink: true }}
            value={quizTitle}
            onChange={(e) => setQuizTitle(e.target.value)}
          />
          <TextField
            select
            fullWidth
            label="Razdoblje"
            // The chosen Period is kept while the list loads; the field only cannot show it yet.
            disabled={!periods}
            value={periods && periodId !== null ? String(periodId) : ""}
            onChange={(e) =>
              setPeriodId(e.target.value ? Number(e.target.value) : null)
            }
            SelectProps={{ displayEmpty: true }}
            InputLabelProps={{ shrink: true }}
            helperText="Kviz se nudi na stranici tog razdoblja."
          >
            <MenuItem value="">Bez razdoblja</MenuItem>
            {periods?.map((period) => (
              <MenuItem key={period.id} value={String(period.id)}>
                {period.name}
              </MenuItem>
            ))}
          </TextField>
          <Box className="quiz-editor-switches">
            <FormControlLabel
              control={
                <Switch
                  checked={isPrivate}
                  // A started Test stays one: the server refuses a change of mode.
                  disabled={locked}
                  onChange={(e) => {
                    setIsPrivate(e.target.checked);
                    // Only a Private Quiz can be a Test.
                    if (!e.target.checked) setIsTest(false);
                  }}
                />
              }
              label="Privatni kviz"
            />
            <Typography className="quiz-editor-hint">
              {isPrivate
                ? "Otvara ga samo tko upiše ulazni kod koji dobijete nakon spremanja."
                : "Vide ga svi, na popisu kvizova."}
            </Typography>
            <FormControlLabel
              control={
                <Checkbox
                  checked={isTest}
                  disabled={!isPrivate || locked}
                  onChange={(e) => setIsTest(e.target.checked)}
                />
              }
              label="Provjera (jedan pokušaj, bez povratne informacije)"
            />
          </Box>
          {hasTimeLimit && (
            <TextField
              type="number"
              fullWidth
              label="Vremensko ograničenje (min)"
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
              label="Zatvara se"
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
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setInputDrawer(false)}>Odustani</Button>
          <Button
            variant="contained"
            disabled={isSaving}
            onClick={() => {
              quizTitle.trim() && timeLimitValid ? saveQuiz() : null;
            }}
          >
            Spremi
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}
