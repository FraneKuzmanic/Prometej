import { Card, CardActionArea, IconButton, Typography } from "@mui/material";
import MoreVertIcon from "@mui/icons-material/MoreVert";
import QuizOutlinedIcon from "@mui/icons-material/QuizOutlined";
import { QuizBaseModel } from "../../types/models/Quiz";
import "./styles.css";

interface Props {
  quiz: QuizBaseModel;
  // the painting of the Quiz's Period, if it has one
  image?: string | null;
  // A Creator's own card says how the Quiz is given out instead of who made it.
  own?: boolean;
  onOpen: () => void;
  onMenu: (anchor: HTMLElement) => void;
}

// "1 pitanje", "21 pitanje", and "pitanja" for every other number.
const questionsLabel = (count: number) =>
  `${count} ${count % 10 === 1 && count % 100 !== 11 ? "pitanje" : "pitanja"}`;

export default function QuizContainer({ quiz, image, own, onOpen, onMenu }: Props) {
  return (
    <Card className="quiz-card">
      <CardActionArea className="quiz-card-open" onClick={onOpen}>
        <span className={image ? "quiz-card-image" : "quiz-card-image plain"}>
          {image ? <img src={`/${image}`} alt="" /> : <QuizOutlinedIcon />}
        </span>
        <span className="quiz-card-text">
          <Typography variant="h6" component="h2" className="quiz-card-title">
            {quiz.title}
          </Typography>
          <span className="quiz-card-meta">
            {[quiz.periodName, questionsLabel(quiz.questionCount)]
              .filter(Boolean)
              .join(" · ")}
          </span>
          {own ? (
            <span className="quiz-card-foot">
              <span className="quiz-card-tag">
                {quiz.isTest ? "Provjera" : quiz.isPrivate ? "Privatni" : "Javni"}
              </span>
              {quiz.entryCode && (
                <span className="quiz-card-code">
                  Ulazni kod: <strong>{quiz.entryCode}</strong>
                </span>
              )}
            </span>
          ) : (
            <span className="quiz-card-foot">
              <span className="quiz-card-creator">{quiz.creatorName}</span>
            </span>
          )}
        </span>
      </CardActionArea>
      <span className="quiz-container-opt">
        <IconButton
          size="small"
          aria-label={`Mogućnosti kviza ${quiz.title}`}
          aria-haspopup="true"
          onClick={(event) => onMenu(event.currentTarget)}
        >
          <MoreVertIcon />
        </IconButton>
      </span>
    </Card>
  );
}
