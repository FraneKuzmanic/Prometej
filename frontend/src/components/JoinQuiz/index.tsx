import {
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
  IconButton,
  TextField,
} from "@mui/material";
import CloseIcon from "@mui/icons-material/Close";
import { FormEvent, useState } from "react";
import { useAppDispatch } from "../../store/store";
import { fetchQuizByCode } from "../../store/slices/quizSlice";
import { useNavigate } from "react-router-dom";
import "./styles.css";

interface JoinQuizProps {
  setOpenJoinQuizDialog: (value: boolean) => void;
}

export default function JoinQuiz({ setOpenJoinQuizDialog }: JoinQuizProps) {
  const [quizCode, setQuizCode] = useState<string>("");
  // what the field says when the code did not open a Quiz
  const [error, setError] = useState<string>("");
  const dispatch = useAppDispatch();
  const navigate = useNavigate();
  const close = () => setOpenJoinQuizDialog(false);

  const joinQuiz = (event: FormEvent) => {
    event.preventDefault();
    dispatch(fetchQuizByCode(quizCode)).then((resultAction) => {
      if (fetchQuizByCode.fulfilled.match(resultAction)) {
        close();
        const { id, isTest } = resultAction.payload;
        // A Test is sat, not played: its Questions come only with a Sitting.
        navigate(`/${isTest ? "sitting" : "play-quiz"}/${id}?code=${quizCode}`);
      } else {
        setError(
          resultAction.payload === 429
            ? "Previše pokušaja. Pričekajte minutu."
            : "Kviz s tim kodom ne postoji."
        );
      }
    });
  };

  return (
    <Dialog
      open
      onClose={close}
      maxWidth="xs"
      fullWidth
      PaperProps={{ component: "form", onSubmit: joinQuiz, className: "join-quiz-form" }}
    >
      <DialogTitle>Pridruži se kvizu</DialogTitle>
      <IconButton className="join-quiz-close" aria-label="Zatvori" onClick={close}>
        <CloseIcon />
      </IconButton>
      <DialogContent>
        <DialogContentText>
          Upišite ulazni kod od pet znamenki koji ste dobili od nastavnika.
        </DialogContentText>
        <TextField
          className="join-quiz-code"
          label="Ulazni kod"
          autoFocus
          fullWidth

          value={quizCode}
          onChange={(e) => {
            setQuizCode(e.target.value);
            setError("");
          }}
          inputProps={{ inputMode: "numeric", autoComplete: "off", maxLength: 10 }}
          error={error !== ""}
          helperText={error || " "}
        />
      </DialogContent>
      <DialogActions>
        <Button onClick={close}>Odustani</Button>
        <Button type="submit" variant="contained" disabled={!quizCode.trim()}>
          Pridruži se
        </Button>
      </DialogActions>
    </Dialog>
  );
}
