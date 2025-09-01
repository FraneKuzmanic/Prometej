import { Avatar, Box, Button, TextField, Typography } from "@mui/material";
import "./styles.css";
import { stringToColor } from "../../components/QuizContainer";

// interface DiscussionGroupProps {
//   title?: string;
// }

interface Data {
  name: string;
  comment: string;
  date: Date;
}

const data = [
  {
    name: "Josip Rakić",
    comment:
      "Nisam baš dobro shvatio lektiru Prijan Lovro, zna li netko koja je osnovna ideja iza te knjige?",
    date: new Date("2024-06-01T10:30:00"),
  },
  {
    name: "Mario Kuzmanić",
    comment:
      "Osnovna ideja je bila prikazati neuspješni romantični život Lovre.",
    date: new Date("2024-06-02T14:54:00"),
  },
  {
    name: "Jakov Kovač",
    comment: "Ideja je bila prikazati život svećenika u 19. stoljeću.",
    date: new Date("2024-06-04T18:12:00"),
  },
  {
    name: "Dino Hrnjić",
    comment:
      "Osnovna ideja je prikazati život mladog ambicioznog čovjeka koji zbog svog društvenog položaja ne uspijeva ispuniti svoj konačan potencijal.",
    date: new Date("2024-06-05T12:32:00"),
  },
];

const formatDate = (date: Date) => {
  const options: Intl.DateTimeFormatOptions = {
    year: "numeric",
    month: "numeric",
    day: "numeric",
    hour: "numeric",
    minute: "numeric",
  };
  return new Date(date).toLocaleDateString("hr-HR", options);
};

export default function DiscussionGroup() {
  return (
    <Box className="forum-wrapper">
      <Box className="forum-comments">
        {data.map((comment: Data, ind) => (
          <Box className="forum-comment" key={ind}>
            <Box className="forum-comment-header">
              <Box className="forum-comment-name">
                <Avatar sx={{ bgcolor: stringToColor(comment.name) }}>
                  {comment.name[0].toUpperCase()}
                </Avatar>
                <Typography
                  sx={{ marginLeft: 1.5, fontSize: 14 }}
                  gutterBottom
                  variant="h6"
                  component="div"
                >
                  {comment.name}
                </Typography>
              </Box>
              <Box className="forum-comment-date">
                {formatDate(comment.date)}
              </Box>
            </Box>
            <Box className="forum-comment-content">{comment.comment}</Box>
          </Box>
        ))}
      </Box>
      <Box className="forum-input-area">
        <TextField
          id="outlined-multiline-static"
          multiline
          placeholder="Napišite komentar..."
          rows={4}
          sx={{ width: "100%" }}
        />
        <Button className="comment-button">Komentiraj</Button>
      </Box>
    </Box>
  );
}
