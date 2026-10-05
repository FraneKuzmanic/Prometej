import { Box, Chip, Typography } from "@mui/material";
import ROLE, { roleLabels } from "../../types/enums/Role";
import { formatDateTime } from "./format";

interface PostAuthorProps {
  // null when the author's account was deleted
  name: string | null;
  role: ROLE | null;
  date: string;
}

// Who wrote a Topic or a Reply and when. A Teacher's and an Admin's post says so.
export default function PostAuthor({ name, role, date }: PostAuthorProps) {
  return (
    <Box className="discussion-author">
      <Typography component="span" className="discussion-author-name">
        {name ?? "Obrisani korisnik"}
      </Typography>
      {(role === ROLE.Teacher || role === ROLE.Admin) && (
        <Chip size="small" label={roleLabels[role]} />
      )}
      <Typography component="span" className="discussion-date">
        {formatDateTime(date)}
      </Typography>
    </Box>
  );
}
