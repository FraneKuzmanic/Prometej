import { IconButton, Tooltip } from "@mui/material";
import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import { useNavigate } from "react-router-dom";

// Leaving a Sitting loses nothing: its answers are on the server and it can be resumed.
// saved: said in the tooltip while a Sitting runs.
export default function BackButton({ saved }: { saved?: boolean }) {
  const navigate = useNavigate();
  // An address opened directly has nowhere to go back to, and leads to the list.
  const leave = () => {
    if (window.history.state?.idx > 0) navigate(-1);
    else navigate("/quizzes");
  };

  return (
    <Tooltip title={saved ? "Natrag (odgovori su spremljeni)" : "Natrag"}>
      <IconButton aria-label="Natrag" className="quiz-play-back" onClick={leave}>
        <ArrowBackIcon />
      </IconButton>
    </Tooltip>
  );
}
