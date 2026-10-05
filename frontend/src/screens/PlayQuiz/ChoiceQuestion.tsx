import { useState } from "react";
import { List, ListItemText } from "@mui/material";
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
    <List className="quiz-play-answers">
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
          <ListItemText
            key={option}
            onClick={() => {
              if (chosenOption === undefined) handleAnswer(option);
            }}
            className={`quiz-play-answer${mark}`}
          >
            {text}
          </ListItemText>
        );
      })}
    </List>
  );
}
