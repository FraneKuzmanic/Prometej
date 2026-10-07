import { useState } from "react";
import CheckIcon from "@mui/icons-material/Check";
import CloseIcon from "@mui/icons-material/Close";
import {
  AnswerCreateRequest,
  QuestionViewModel,
} from "../../types/models/Quiz";

interface ChoiceQuestionProps {
  question: QuestionViewModel;
  onAnswered: (
    answer: AnswerCreateRequest,
    points: number,
    maxPoints: number
  ) => void;
}

// The four options are lettered, as on the exam.
const letters = ["A", "B", "C", "D"];

export default function ChoiceQuestion({
  question,
  onAnswered,
}: ChoiceQuestionProps) {
  // which of the four answers was clicked, 1 to 4; undefined until the Question is answered
  const [chosenOption, setChosenOption] = useState<number>();

  const handleAnswer = (option: number) => {
    setChosenOption(option);
    onAnswered(
      { questionId: question.id, chosenOption: option },
      option === question.correctOption ? 1 : 0,
      1
    );
  };

  return (
    <div className="quiz-play-answers">
      {[
        question.firstAnswer,
        question.secondAnswer,
        question.thirdAnswer,
        question.fourthAnswer,
      ].map((text, index) => {
        const option = index + 1;
        // Once answered, the correct option turns green and a wrong choice red.
        const mark =
          chosenOption === undefined
            ? ""
            : option === question.correctOption
            ? "-correct"
            : option === chosenOption
            ? "-uncorrect"
            : "";
        return (
          <button
            key={option}
            type="button"
            disabled={chosenOption !== undefined}
            onClick={() => handleAnswer(option)}
            className={`quiz-play-answer${mark}`}
          >
            <span className="quiz-play-letter" aria-hidden="true">
              {letters[index]}
            </span>
            <span className="quiz-play-item-text">{text}</span>
            {mark === "-correct" && <CheckIcon fontSize="small" titleAccess="Točno" />}
            {mark === "-uncorrect" && <CloseIcon fontSize="small" titleAccess="Netočno" />}
          </button>
        );
      })}
    </div>
  );
}
