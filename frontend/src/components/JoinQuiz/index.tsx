import { Button, Paper, TextField, Typography } from "@mui/material";
import { useState } from "react";
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

  const joinQuiz = () => {
    dispatch(fetchQuizByCode(quizCode)).then((resultAction) => {
      if (fetchQuizByCode.fulfilled.match(resultAction)) {
        setOpenJoinQuizDialog(false);
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
    <Paper elevation={10} className="join-quiz-form">
      <Typography sx={{ fontWeight: "bold", marginTop: "1rem" }} variant="h5">
        Pridruži se kvizu
      </Typography>
      <Typography sx={{ marginTop: "3.5rem" }} variant="h5">
        Unesite kod
      </Typography>
      <TextField
        sx={{ marginTop: "0.5rem" }}
        type="text"
        placeholder="Privatni kod..."
        fullWidth
        required
        value={quizCode}
        onChange={(e) => {
          setQuizCode(e.target.value);
          setError("");
        }}
        error={error !== ""}
        helperText={error}
      />
      <Button
        variant="contained"
        onClick={() => joinQuiz()}
        style={{
          backgroundColor: "#553b08",
          marginTop: "2rem",
          width: "80%",
        }}
      >
        Pridruži se
      </Button>
      <Button
        variant="contained"
        style={{
          backgroundColor: "#553b08",
          marginTop: "1rem",
          width: "80%",
        }}
        onClick={() => setOpenJoinQuizDialog(false)}
      >
        Odustani
      </Button>
    </Paper>
  );
}
