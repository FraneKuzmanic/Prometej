import { IconButton } from "@mui/material";
import { TUTOR_AVATAR } from "./avatar";
import "./styles.css";

interface TutorButtonProps {
  // Moved left of the Admin's own button on a Period page.
  shifted: boolean;
  onClick: () => void;
}

export default function TutorButton({ shifted, onClick }: TutorButtonProps) {
  return (
    <IconButton
      className="tutor-button"
      aria-label="Pitaj Prometeja"
      onClick={onClick}
      sx={{ position: "fixed", bottom: 16, right: shifted ? 88 : 16 }}
    >
      <img src={TUTOR_AVATAR} alt="" />
    </IconButton>
  );
}
