import { useState } from "react";
import { Box, Button, Typography } from "@mui/material";
import CheckIcon from "@mui/icons-material/Check";
import CloseIcon from "@mui/icons-material/Close";
import {
  AnswerCreateRequest,
  QuestionViewModel,
} from "../../types/models/Quiz";
import { shuffledOutOfOrder } from "./shuffle";

interface OrderingQuestionProps {
  question: QuestionViewModel;
  onAnswered: (
    answer: AnswerCreateRequest,
    points: number,
    maxPoints: number
  ) => void;
}

// Tap an item of the pool to put it in the first free place; tap a placed item to send it
// back. "Provjeri" marks every place at once and gives a point for each item at its place.
export default function OrderingQuestion({
  question,
  onAnswered,
}: OrderingQuestionProps) {
  // Stored in the right order: item i belongs to place i.
  const items = question.content?.items ?? [];
  // The order the pool shows them in, made once for this play of the Question.
  const [poolOrder] = useState(() => shuffledOutOfOrder(items.length));
  // for each place, the item put there
  const [places, setPlaces] = useState<(number | null)[]>(() =>
    items.map(() => null)
  );
  const [checked, setChecked] = useState(false);

  const place = (item: number) => {
    const free = places.indexOf(null);
    setPlaces(places.map((placed, i) => (i === free ? item : placed)));
  };

  const takeBack = (index: number) =>
    setPlaces(places.map((placed, i) => (i === index ? null : placed)));

  const handleCheck = () => {
    setChecked(true);
    onAnswered(
      { questionId: question.id, order: places.map((item) => item! + 1) },
      places.filter((item, index) => item === index).length,
      items.length
    );
  };

  return (
    <Box className="quiz-play-ordering">
      <ol className="quiz-play-places">
        {places.map((item, index) => (
          <li key={index}>
            <button
              type="button"
              className={`quiz-play-item${
                item === null
                  ? " empty"
                  : checked
                  ? item === index
                    ? " correct"
                    : " wrong"
                  : ""
              }`}
              aria-label={
                item === null
                  ? `${index + 1}. mjesto, prazno`
                  : `${index + 1}. mjesto: ${items[item]}`
              }
              disabled={checked || item === null}
              onClick={() => takeBack(index)}
            >
              <span className="quiz-play-item-badge">{index + 1}</span>
              <span className="quiz-play-item-text">
                {item === null ? "" : items[item]}
              </span>
              {checked &&
                (item === index ? (
                  <CheckIcon fontSize="small" titleAccess="Točno" />
                ) : (
                  <CloseIcon fontSize="small" titleAccess="Netočno" />
                ))}
            </button>
            {checked && item !== index && (
              <Typography variant="body2" className="quiz-play-item-right">
                Točan odgovor: {items[index]}
              </Typography>
            )}
          </li>
        ))}
      </ol>
      {!checked && (
        <>
          <Box className="quiz-play-pool">
            {poolOrder
              .filter((item) => !places.includes(item))
              .map((item) => (
                <button
                  key={item}
                  type="button"
                  className="quiz-play-item"
                  onClick={() => place(item)}
                >
                  <span className="quiz-play-item-text">{items[item]}</span>
                </button>
              ))}
          </Box>
          <Button
            variant="contained"
            className="quiz-play-check"
            disabled={places.includes(null)}
            onClick={handleCheck}
          >
            Provjeri
          </Button>
        </>
      )}
    </Box>
  );
}
