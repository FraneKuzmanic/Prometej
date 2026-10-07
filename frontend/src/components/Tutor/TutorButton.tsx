import { ButtonBase } from "@mui/material";
import { TUTOR_AVATAR } from "./avatar";
import "./styles.css";

export default function TutorButton({ onClick }: { onClick: () => void }) {
  return (
    <ButtonBase
      className="tutor-button"
      onClick={onClick}
      sx={{ position: "fixed", bottom: 16, right: 16 }}
    >
      <img src={TUTOR_AVATAR} alt="" />
      Pitaj Prometeja
    </ButtonBase>
  );
}
