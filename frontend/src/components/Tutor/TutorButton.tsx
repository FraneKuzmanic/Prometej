import { ButtonBase } from "@mui/material";
import { TUTOR_AVATAR } from "./avatar";
import "./styles.css";

interface TutorButtonProps {
  // Moved left of the Admin's own button on a Period page.
  shifted: boolean;
  onClick: () => void;
}

export default function TutorButton({ shifted, onClick }: TutorButtonProps) {
  return (
    <ButtonBase
      className="tutor-button"
      onClick={onClick}
      sx={{ position: "fixed", bottom: 16, right: shifted ? 88 : 16 }}
    >
      <img src={TUTOR_AVATAR} alt="" />
      Pitaj Prometeja
    </ButtonBase>
  );
}
