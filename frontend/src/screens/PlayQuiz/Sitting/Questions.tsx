import { useCallback, useRef, useState } from "react";
import {
  Alert,
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
  Paper,
  Typography,
} from "@mui/material";
import { Sitting, SittingQuestion } from "../../../types/models/Sitting";
import { useAppDispatch } from "../../../store/store";
import {
  finishSitting,
  saveSittingAnswer,
} from "../../../store/slices/sittingSlice";
import SourceTextPanel from "../SourceTextPanel";
import ChoiceAnswer from "./ChoiceAnswer";
import MatchingAnswer from "./MatchingAnswer";
import OrderingAnswer from "./OrderingAnswer";
import Countdown from "./Countdown";

interface QuestionsProps {
  sitting: Sitting;
  // The server ended the Sitting while the user was still working on it.
  onEndedByServer: () => void;
  // The Quiz was edited meanwhile and the Sitting went with the old Questions.
  onDiscardedByEdit: () => void;
}

const emptyAnswer = (question: SittingQuestion) =>
  new Array<number>(
    question.type === "matching"
      ? question.lefts?.length ?? 0
      : question.type === "ordering"
      ? question.items?.length ?? 0
      : 1
  ).fill(0);

const isAnswered = (given: number[]) => given.some((number) => number !== 0);

// A running Sitting: any Question at any time, every answer saved as it is given, and one
// "Predaj" at the end. Only the answers live here; the Questions are the server's.
export default function Questions({
  sitting,
  onEndedByServer,
  onDiscardedByEdit,
}: QuestionsProps) {
  const dispatch = useAppDispatch();
  const [answers, setAnswers] = useState<Record<number, number[]>>(() =>
    Object.fromEntries(
      sitting.questions.map((question) => [
        question.questionId,
        question.given ?? emptyAnswer(question),
      ])
    )
  );
  const [current, setCurrent] = useState(0);
  // "idle" until the first answer is given: nothing was saved yet.
  const [saveState, setSaveState] = useState<
    "idle" | "saved" | "saving" | "failed"
  >("idle");
  // The server has ended the Sitting and its result could not be read.
  const [resultFailed, setResultFailed] = useState(false);
  const [submitOpen, setSubmitOpen] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [submitFailed, setSubmitFailed] = useState(false);
  const [serverOffset] = useState(
    () => new Date(sitting.serverNow).getTime() - Date.now()
  );
  // One request at a time, each with the latest answer of its Question: two quick changes
  // to one Question could otherwise arrive in the wrong order and the older would be kept.
  const unsent = useRef(new Map<number, number[]>());
  const sending = useRef<Promise<void> | null>(null);

  // The Sitting is over on the server: its time ran out or its Test was closed. Asking to
  // finish it is how its result is read.
  const readResult = useCallback(async () => {
    onEndedByServer();
    setResultFailed(false);
    const result = await dispatch(finishSitting(sitting.id));
    if (finishSitting.rejected.match(result)) {
      if (result.payload === 404) {
        onDiscardedByEdit();
      } else {
        setResultFailed(true);
      }
    }
  }, [dispatch, sitting.id, onEndedByServer, onDiscardedByEdit]);

  const sendUnsent = async () => {
    setSaveState("saving");
    while (unsent.current.size > 0) {
      const [questionId, given] = unsent.current.entries().next().value!;
      unsent.current.delete(questionId);
      const result = await dispatch(
        saveSittingAnswer({ sittingId: sitting.id, answer: { questionId, given } })
      );
      if (saveSittingAnswer.rejected.match(result)) {
        if (result.payload === 409) {
          readResult();
        } else if (result.payload === 404) {
          onDiscardedByEdit();
        } else {
          // Kept for the next try, unless a newer answer to the Question came meanwhile.
          if (!unsent.current.has(questionId)) {
            unsent.current.set(questionId, given);
          }
          setSaveState("failed");
        }
        return;
      }
    }
    setSaveState("saved");
  };

  const save = () => {
    if (!sending.current) {
      sending.current = sendUnsent().finally(() => {
        sending.current = null;
      });
    }
    return sending.current;
  };

  const handleChange = (questionId: number, given: number[]) => {
    setAnswers((previous) => ({ ...previous, [questionId]: given }));
    unsent.current.set(questionId, given);
    save();
  };

  const submit = async () => {
    setSubmitting(true);
    setSubmitFailed(false);
    await save();
    // An answer that could not be saved stays on the screen with its message.
    if (unsent.current.size > 0) {
      setSubmitOpen(false);
    } else {
      const result = await dispatch(finishSitting(sitting.id));
      // Fulfilled, the store drops the Sitting and this screen is replaced by the result.
      if (finishSitting.rejected.match(result)) {
        if (result.payload === 404) {
          onDiscardedByEdit();
        } else {
          setSubmitFailed(true);
        }
      }
    }
    setSubmitting(false);
  };

  const question = sitting.questions[current];
  const given = answers[question.questionId];
  const sourceText = sitting.sourceTexts.find(
    (text) => text.id === question.sourceTextId
  );
  const unanswered = sitting.questions
    .map((q, index) => (isAnswered(answers[q.questionId]) ? 0 : index + 1))
    .filter((number) => number !== 0);
  const answerProps = {
    question,
    given,
    onChange: (changed: number[]) => handleChange(question.questionId, changed),
  };

  return (
    <Paper
      elevation={3}
      className={`quiz-play-container${sourceText ? " with-source-text" : ""}`}
    >
      <Box className="quiz-play-header">
        <Box className="sitting-header">
          <Typography className="quiz-play-title" variant="h5">
            {sitting.quizTitle}
          </Typography>
          {sitting.endsAt && (
            <Countdown
              endsAt={sitting.endsAt}
              offset={serverOffset}
              onZero={readResult}
            />
          )}
        </Box>
        <Box className="sitting-strip">
          {sitting.questions.map((q, index) => {
            const answered = isAnswered(answers[q.questionId]);
            return (
              <button
                key={q.questionId}
                type="button"
                className={`sitting-strip-item${answered ? " answered" : ""}${
                  index === current ? " current" : ""
                }`}
                aria-label={`Pitanje ${index + 1}, ${
                  answered ? "odgovoreno" : "nije odgovoreno"
                }`}
                aria-current={index === current ? "step" : undefined}
                onClick={() => setCurrent(index)}
              >
                {index + 1}
              </button>
            );
          })}
        </Box>
      </Box>
      {/* Keyed by the text, so it stays as the user left it through its Questions. */}
      {sourceText && (
        <SourceTextPanel key={sourceText.id} sourceText={sourceText} />
      )}
      <Box className="quiz-play-content">
        <Typography variant="h4" className="quiz-play-question">
          {current + 1}. {question.questionTitle}
        </Typography>
        {question.type === "matching" && (
          <Typography className="sitting-instruction">
            Odaberite pojam lijevo, zatim njegov par desno.
          </Typography>
        )}
        {question.type === "ordering" && (
          <Typography className="sitting-instruction">
            Dodirnite stavke redom kojim idu.
          </Typography>
        )}
        {/* The key gives every Question its own component; the answers are held above. */}
        {question.type === "choice" && (
          <ChoiceAnswer key={question.questionId} {...answerProps} />
        )}
        {question.type === "matching" && (
          <MatchingAnswer key={question.questionId} {...answerProps} />
        )}
        {question.type === "ordering" && (
          <OrderingAnswer key={question.questionId} {...answerProps} />
        )}
        {resultFailed && (
          <Alert
            severity="warning"
            action={
              <Button color="inherit" size="small" onClick={() => readResult()}>
                Pokušaj ponovno
              </Button>
            }
          >
            Vrijeme je isteklo, a rezultat se nije učitao.
          </Alert>
        )}
        <Box className="sitting-save-state" aria-live="polite">
          {saveState === "saved" && (
            <Typography variant="body2">Spremljeno</Typography>
          )}
          {saveState === "saving" && (
            <Typography variant="body2">Spremanje…</Typography>
          )}
          {saveState === "failed" && (
            <>
              <Typography variant="body2" color="error">
                Odgovor nije spremljen. Pokušajte ponovno.
              </Typography>
              <Button size="small" onClick={() => save()}>
                Pokušaj ponovno
              </Button>
            </>
          )}
        </Box>
      </Box>
      <Box className="quiz-play-footer">
        <Box sx={{ display: "flex", gap: 1 }}>
          <Button
            variant="outlined"
            disabled={current === 0}
            onClick={() => setCurrent(current - 1)}
          >
            Prethodno
          </Button>
          <Button
            variant="outlined"
            disabled={current === sitting.questions.length - 1}
            onClick={() => setCurrent(current + 1)}
          >
            Sljedeće
          </Button>
        </Box>
        <Button variant="contained" onClick={() => setSubmitOpen(true)}>
          Predaj
        </Button>
      </Box>
      <Dialog open={submitOpen} onClose={() => setSubmitOpen(false)}>
        <DialogTitle>Predaja provjere</DialogTitle>
        <DialogContent>
          <DialogContentText>
            Nakon predaje odgovore više ne možete mijenjati.
            {unanswered.length > 0 &&
              ` Niste odgovorili na pitanja: ${unanswered.join(", ")}.`}
          </DialogContentText>
          {submitFailed && (
            <Typography color="error" sx={{ marginTop: 1 }}>
              Predaja nije uspjela. Pokušajte ponovno.
            </Typography>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setSubmitOpen(false)}>Odustani</Button>
          <Button disabled={submitting} onClick={() => submit()}>
            Predaj
          </Button>
        </DialogActions>
      </Dialog>
    </Paper>
  );
}
