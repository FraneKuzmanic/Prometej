import { Link, Typography } from "@mui/material";
import { Link as RouterLink } from "react-router-dom";

// Shown where the form would be: reading needs no account, writing does.
export default function SignInPrompt() {
  return (
    <Typography className="discussion-actions">
      <Link component={RouterLink} to="/login">
        Prijavite se
      </Link>{" "}
      da biste sudjelovali u raspravi.
    </Typography>
  );
}
