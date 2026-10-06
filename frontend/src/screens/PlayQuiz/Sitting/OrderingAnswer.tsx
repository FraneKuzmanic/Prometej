import { Box } from "@mui/material";
import { AnswerProps } from "./ChoiceAnswer";

// Tap an item of the pool to put it in the first free place; tap a placed item to send it
// back. The pool is in the order the server sent the items, and given[i] is the number,
// from 1, of the item at place i.
export default function OrderingAnswer({ question, given, onChange }: AnswerProps) {
  const items = question.items ?? [];

  const place = (item: number) => {
    const free = given.indexOf(0);
    onChange(given.map((placed, i) => (i === free ? item : placed)));
  };

  const takeBack = (index: number) =>
    onChange(given.map((placed, i) => (i === index ? 0 : placed)));

  return (
    <Box className="quiz-play-ordering">
      <ol className="quiz-play-places">
        {given.map((item, index) => (
          <li key={index}>
            <button
              type="button"
              className={`quiz-play-item${item === 0 ? " empty" : ""}`}
              aria-label={
                item === 0
                  ? `${index + 1}. mjesto, prazno`
                  : `${index + 1}. mjesto: ${items[item - 1]}`
              }
              disabled={item === 0}
              onClick={() => takeBack(index)}
            >
              <span className="quiz-play-item-badge">{index + 1}</span>
              <span className="quiz-play-item-text">
                {item === 0 ? "" : items[item - 1]}
              </span>
            </button>
          </li>
        ))}
      </ol>
      <Box className="quiz-play-pool">
        {items.map(
          (text, index) =>
            !given.includes(index + 1) && (
              <button
                key={index}
                type="button"
                className="quiz-play-item"
                onClick={() => place(index + 1)}
              >
                <span className="quiz-play-item-text">{text}</span>
              </button>
            )
        )}
      </Box>
    </Box>
  );
}
