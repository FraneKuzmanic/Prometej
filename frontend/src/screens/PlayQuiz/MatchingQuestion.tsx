import { useState } from "react";
import { Box, Button, Typography } from "@mui/material";
import CheckIcon from "@mui/icons-material/Check";
import CloseIcon from "@mui/icons-material/Close";
import {
  AnswerCreateRequest,
  QuestionViewModel,
} from "../../types/models/Quiz";
import { shuffledOutOfOrder } from "./shuffle";

interface MatchingQuestionProps {
  question: QuestionViewModel;
  onAnswered: (
    answer: AnswerCreateRequest,
    points: number,
    maxPoints: number
  ) => void;
}

// Click a left item, then the right-hand option that goes with it; a click on either end of
// a link undoes it. "Provjeri" marks every pair at once and gives a point for each right one.
export default function MatchingQuestion({
  question,
  onAnswered,
}: MatchingQuestionProps) {
  const pairs = question.content?.pairs ?? [];
  // The right-hand options as the server numbers them: the pairs' own, then the extras.
  // Option i belongs to left item i; an extra belongs to none.
  const rightOptions = [
    ...pairs.map((pair) => pair.right),
    ...(question.content?.extras ?? []),
  ];
  // The order they are shown in, made once for this play of the Question.
  const [shownOrder] = useState(() =>
    shuffledOutOfOrder(rightOptions.length)
  );
  // for each left item, the right-hand option linked to it
  const [links, setLinks] = useState<(number | null)[]>(() =>
    pairs.map(() => null)
  );
  // the left item waiting for its right-hand option
  const [activeLeft, setActiveLeft] = useState<number | null>(null);
  const [checked, setChecked] = useState(false);

  const setLink = (left: number, right: number | null) =>
    setLinks(links.map((link, i) => (i === left ? right : link)));

  const handleLeft = (left: number) => {
    if (links[left] !== null) {
      setLink(left, null);
      setActiveLeft(null);
    } else {
      setActiveLeft(activeLeft === left ? null : left);
    }
  };

  const handleRight = (right: number) => {
    const linkedLeft = links.indexOf(right);
    if (linkedLeft !== -1) {
      setLink(linkedLeft, null);
    } else if (activeLeft !== null) {
      setLink(activeLeft, right);
      setActiveLeft(null);
    }
  };

  const handleCheck = () => {
    setChecked(true);
    onAnswered(
      { questionId: question.id, matches: links.map((link) => link! + 1) },
      links.filter((link, left) => link === left).length,
      pairs.length
    );
  };

  // A linked pair shares a number and a colour; after the check the colour says right or wrong.
  const mark = (left: number) => {
    if (!checked) return ` linked link-${left + 1}`;
    return links[left] === left ? " correct" : " wrong";
  };

  return (
    <Box className="quiz-play-matching">
      <Box className="quiz-play-matching-columns">
        <Box className="quiz-play-matching-column">
          {pairs.map((pair, left) => (
            <Box key={left}>
              <button
                type="button"
                className={`quiz-play-item${
                  links[left] !== null ? mark(left) : ""
                }${activeLeft === left ? " active" : ""}`}
                aria-pressed={activeLeft === left || links[left] !== null}
                disabled={checked}
                onClick={() => handleLeft(left)}
              >
                <span className="quiz-play-item-badge">
                  {links[left] !== null ? left + 1 : ""}
                </span>
                <span className="quiz-play-item-text">{pair.left}</span>
                {checked &&
                  (links[left] === left ? (
                    <CheckIcon fontSize="small" titleAccess="Točno" />
                  ) : (
                    <CloseIcon fontSize="small" titleAccess="Netočno" />
                  ))}
              </button>
              {checked && links[left] !== left && (
                <Typography variant="body2" className="quiz-play-item-right">
                  Točan odgovor: {pair.right}
                </Typography>
              )}
            </Box>
          ))}
        </Box>
        <Box className="quiz-play-matching-column">
          {shownOrder.map((right) => {
            const linkedLeft = links.indexOf(right);
            return (
              <button
                key={right}
                type="button"
                className={`quiz-play-item${
                  linkedLeft !== -1 ? mark(linkedLeft) : ""
                }`}
                aria-pressed={linkedLeft !== -1}
                disabled={checked}
                onClick={() => handleRight(right)}
              >
                <span className="quiz-play-item-badge">
                  {linkedLeft !== -1 ? linkedLeft + 1 : ""}
                </span>
                <span className="quiz-play-item-text">{rightOptions[right]}</span>
              </button>
            );
          })}
        </Box>
      </Box>
      {!checked && (
        <Button
          variant="contained"
          className="quiz-play-check"
          disabled={links.includes(null)}
          onClick={handleCheck}
        >
          Provjeri
        </Button>
      )}
    </Box>
  );
}
