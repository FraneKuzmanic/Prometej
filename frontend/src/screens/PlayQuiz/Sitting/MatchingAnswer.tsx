import { useState } from "react";
import { Box } from "@mui/material";
import { AnswerProps } from "./ChoiceAnswer";

// Click a left item, then the right-hand option that goes with it; a click on either end of
// a link undoes it. The right-hand options are in the order the server sent them, and
// given[i] is the number, from 1, of the one linked to left item i.
export default function MatchingAnswer({ question, given, onChange }: AnswerProps) {
  const lefts = question.lefts ?? [];
  const rights = question.rights ?? [];
  // the left item waiting for its right-hand option
  const [activeLeft, setActiveLeft] = useState<number | null>(null);

  const setLink = (left: number, right: number) =>
    onChange(given.map((linked, i) => (i === left ? right : linked)));

  const handleLeft = (left: number) => {
    if (given[left] !== 0) {
      setLink(left, 0);
      setActiveLeft(null);
    } else {
      setActiveLeft(activeLeft === left ? null : left);
    }
  };

  const handleRight = (right: number) => {
    const linkedLeft = given.indexOf(right);
    if (linkedLeft !== -1) {
      setLink(linkedLeft, 0);
    } else if (activeLeft !== null) {
      setLink(activeLeft, right);
      setActiveLeft(null);
    }
  };

  return (
    <Box className="quiz-play-matching">
      <Box className="quiz-play-matching-columns">
        <Box className="quiz-play-matching-column">
          {lefts.map((text, left) => (
            <button
              key={left}
              type="button"
              className={`quiz-play-item${
                given[left] !== 0 ? ` linked link-${left + 1}` : ""
              }${activeLeft === left ? " active" : ""}`}
              aria-pressed={activeLeft === left || given[left] !== 0}
              onClick={() => handleLeft(left)}
            >
              <span className="quiz-play-item-badge">
                {given[left] !== 0 ? left + 1 : ""}
              </span>
              <span className="quiz-play-item-text">{text}</span>
            </button>
          ))}
        </Box>
        <Box className="quiz-play-matching-column">
          {rights.map((text, index) => {
            const linkedLeft = given.indexOf(index + 1);
            return (
              <button
                key={index}
                type="button"
                className={`quiz-play-item${
                  linkedLeft !== -1 ? ` linked link-${linkedLeft + 1}` : ""
                }`}
                aria-pressed={linkedLeft !== -1}
                onClick={() => handleRight(index + 1)}
              >
                <span className="quiz-play-item-badge">
                  {linkedLeft !== -1 ? linkedLeft + 1 : ""}
                </span>
                <span className="quiz-play-item-text">{text}</span>
              </button>
            );
          })}
        </Box>
      </Box>
    </Box>
  );
}
