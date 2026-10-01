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
  const [notFound, setNotFound] = useState<boolean>(false);
  const dispatch = useAppDispatch();
  const navigate = useNavigate();

  const joinQuiz = () => {
    dispatch(fetchQuizByCode(quizCode)).then((resultAction) => {
      if (fetchQuizByCode.fulfilled.match(resultAction)) {
        setOpenJoinQuizDialog(false);
        navigate(`/play-quiz/${resultAction.payload.id}?code=${quizCode}`);
      } else {
        setNotFound(true);
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
          setNotFound(false);
        }}
        error={notFound}
        helperText={notFound ? "Kviz s tim kodom ne postoji." : ""}
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
