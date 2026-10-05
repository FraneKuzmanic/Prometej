import { Box, Typography } from "@mui/material";
import { AnswerViewModel } from "../../types/models/Quiz";
import "./styles.css";

interface AnswerGroupProps {
  // the Question's number in the Quiz Game
  number: number;
  // the Answer rows of that one Question
  answers: AnswerViewModel[];
  // "Vaš odgovor" for the player, "Odgovor" for the Creator
  answerLabel: string;
  // smaller type, for a group shown inside a table
  dense?: boolean;
  showExploreMore?: boolean;
}

// One Question of a Quiz Game as it was played: what was asked, what was answered and, where
// that was wrong, what was right. It reads only the Answers, never the Question of today, and
// tells the Question's kind from them: a row with an item is a pair of a matching Question,
// a row with a place is a place of an ordering one.
export default function AnswerGroup({
  number,
  answers,
  answerLabel,
  dense,
  showExploreMore,
}: AnswerGroupProps) {
  const [first] = answers;
  const body = dense ? "body2" : "body1";

  return (
    <>
      <Typography variant={dense ? "subtitle2" : "h6"}>
        {number}. {first.questionTitle}
      </Typography>
      {answers.map((answer) => {
        const correct = answer.answerText === answer.correctAnswer;
        return (
          <Box key={answer.id}>
            <Typography
              variant={body}
              className={correct ? "answer-group-correct" : "answer-group-wrong"}
            >
              {answer.item !== null
                ? `${answer.item} → ${answer.answerText}`
                : answer.place !== null
                ? `${answer.place}. mjesto: ${answer.answerText}`
                : `${answerLabel}: ${answer.answerText}`}
            </Typography>
            {!correct && (
              <Typography variant={body}>
                Točan odgovor: {answer.correctAnswer}
              </Typography>
            )}
          </Box>
        );
      })}
      {showExploreMore && first.exploreMore && (
        <Box className="answer-group-explore">
          <Typography variant="subtitle2">Saznaj više</Typography>
          <Typography>{first.exploreMore}</Typography>
        </Box>
      )}
    </>
  );
}
