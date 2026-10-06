import { useEffect, useState } from "react";
import { useSelector } from "react-redux";
import axios from "axios";
import {
  Alert,
  Box,
  Button,
  Checkbox,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  MenuItem,
  TextField,
  Typography,
} from "@mui/material";
import { RootState } from "../../store/store";
import periodService from "../../services/routes/period";
import tutorService from "../../services/routes/tutor";
import { TutorDraft, TutorDrafts } from "../../types/models/Tutor";
import { ContentsEntry, withHeadingIds } from "../ContentsList/headings";
import { EditorQuestion, newQuestion } from "./questions";

interface SuggestQuestionsProps {
  // The Period chosen for the Quiz, offered first.
  initialPeriodId: number | null;
  onAdd: (questions: EditorQuestion[]) => void;
  onClose: () => void;
}

const answersOf = (draft: TutorDraft) => [
  draft.firstAnswer,
  draft.secondAnswer,
  draft.thirdAnswer,
  draft.fourthAnswer,
];

// A draft has no Hint and no Explore More: what a model wrote unchecked does not go into a
// field a Student reads. Its quote stays in this dialog.
const toQuestion = (draft: TutorDraft): EditorQuestion => ({
  ...newQuestion("choice"),
  questionTitle: draft.questionTitle,
  firstAnswer: draft.firstAnswer,
  secondAnswer: draft.secondAnswer,
  thirdAnswer: draft.thirdAnswer,
  fourthAnswer: draft.fourthAnswer,
  correctOption: draft.correctOption,
});

// Drafts of four-option Questions from one section of a Period's text. Nothing is saved
// here: the chosen drafts become Questions of the editor, to change and to save as any other.
export default function SuggestQuestions({ initialPeriodId, onAdd, onClose }: SuggestQuestionsProps) {
  const { periods } = useSelector((state: RootState) => state.period);
  const [periodId, setPeriodId] = useState<string>(
    initialPeriodId === null ? "" : String(initialPeriodId)
  );
  // The sections of the chosen Period's text; undefined while they load.
  const [sections, setSections] = useState<ContentsEntry[]>();
  const [sectionId, setSectionId] = useState("");
  const [pending, setPending] = useState(false);
  const [error, setError] = useState<"limit" | "other">();
  const [result, setResult] = useState<TutorDrafts>();
  const [chosen, setChosen] = useState<boolean[]>([]);

  useEffect(() => {
    setSections(undefined);
    setSectionId("");
    if (periodId === "") return;
    let current = true;
    periodService
      .get(periodId)
      .then((response) => {
        // The ids are the ones the Period page gives the same headings.
        if (current) setSections(withHeadingIds(response.data.content).entries);
      })
      .catch(() => {
        // A Period without a text has nothing to draft from.
        if (current) setSections([]);
      });
    return () => {
      current = false;
    };
  }, [periodId]);

  const suggest = async () => {
    setPending(true);
    setError(undefined);
    setResult(undefined);
    try {
      const response = await tutorService.drafts({ periodId: Number(periodId), sectionId });
      const drafts: TutorDrafts = response.data;
      setResult(drafts);
      setChosen(drafts.drafts.map(() => true));
    } catch (failure) {
      const status = axios.isAxiosError(failure) ? failure.response?.status : undefined;
      setError(status === 429 ? "limit" : "other");
    }
    setPending(false);
  };

  const drafts = result?.drafts ?? [];
  const chosenDrafts = drafts.filter((_, index) => chosen[index]);

  return (
    <Dialog open onClose={onClose} fullWidth maxWidth="md" aria-labelledby="suggest-title">
      <DialogTitle id="suggest-title">Predloži pitanja</DialogTitle>
      <DialogContent>
        <Typography sx={{ marginBottom: 2 }}>
          Prometej iz odabranog poglavlja gradiva sastavlja do pet pitanja s četiri odgovora.
          Prijedloge pregledajte: dodaju se u kviz tek kad ih odaberete, a spremaju kad
          spremite kviz.
        </Typography>
        <Box className="suggest-fields">
          <TextField
            select
            fullWidth
            size="small"
            label="Razdoblje"
            disabled={!periods || pending}
            value={periods ? periodId : ""}
            onChange={(event) => setPeriodId(event.target.value)}
          >
            {(periods ?? []).map((period) => (
              <MenuItem key={period.id} value={String(period.id)}>
                {period.name}
              </MenuItem>
            ))}
          </TextField>
          <TextField
            select
            fullWidth
            size="small"
            label="Poglavlje"
            disabled={!sections || sections.length === 0 || pending}
            value={sectionId}
            onChange={(event) => setSectionId(event.target.value)}
            helperText={
              periodId !== "" && sections?.length === 0 ? "Za ovo razdoblje nema gradiva." : " "
            }
          >
            {(sections ?? []).map((section) => (
              <MenuItem
                key={section.id}
                value={section.id}
                sx={{ paddingLeft: section.level === 3 ? 4 : 2 }}
              >
                {section.text}
              </MenuItem>
            ))}
          </TextField>
          <Button
            variant="contained"
            style={{ backgroundColor: "#553b08" }}
            disabled={sectionId === "" || pending}
            onClick={suggest}
          >
            Predloži
          </Button>
        </Box>

        {pending && <Typography role="status">Prometej sastavlja pitanja…</Typography>}
        {error !== undefined && (
          <Typography color="error" role="alert">
            {error === "limit"
              ? "Previše zahtjeva u kratkom vremenu. Pokušajte ponovno za minutu."
              : "Prometej trenutno nije dostupan. Pokušajte ponovno."}
          </Typography>
        )}
        {result && drafts.length === 0 && (
          <Typography>Iz ovog poglavlja nije nastao nijedan prijedlog.</Typography>
        )}
        {drafts.map((draft, index) => (
          <Box key={index} className="suggest-draft">
            <Checkbox
              checked={chosen[index] ?? false}
              onChange={(event) =>
                setChosen(chosen.map((value, i) => (i === index ? event.target.checked : value)))
              }
              inputProps={{ "aria-label": `Dodaj prijedlog ${index + 1}` }}
            />
            <Box className="suggest-draft-body">
              <Typography className="suggest-draft-title">{draft.questionTitle}</Typography>
              <ol>
                {answersOf(draft).map((answer, option) => (
                  <li
                    key={option}
                    className={option + 1 === draft.correctOption ? "suggest-correct" : undefined}
                  >
                    {answer}
                    {option + 1 === draft.correctOption && " (točan odgovor)"}
                  </li>
                ))}
              </ol>
              <Typography className="suggest-quote-title">Iz gradiva</Typography>
              <blockquote>{draft.quote}</blockquote>
              {!draft.agrees && (
                <Alert severity="warning" sx={{ marginTop: 1 }}>
                  Provjerite ovo pitanje: druga provjera nije dala isti odgovor.
                </Alert>
              )}
            </Box>
          </Box>
        ))}
        {result && result.dropped > 0 && (
          <Typography className="suggest-dropped">
            Odbačeno prijedloga: {result.dropped}
          </Typography>
        )}
      </DialogContent>
      <DialogActions>
        <Button sx={{ color: "#553b08" }} onClick={onClose}>
          Odustani
        </Button>
        <Button
          variant="contained"
          style={chosenDrafts.length > 0 ? { backgroundColor: "#553b08" } : undefined}
          disabled={chosenDrafts.length === 0}
          onClick={() => onAdd(chosenDrafts.map(toQuestion))}
        >
          Dodaj odabrana
        </Button>
      </DialogActions>
    </Dialog>
  );
}
