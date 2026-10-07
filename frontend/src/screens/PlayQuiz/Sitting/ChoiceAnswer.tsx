import { SittingQuestion } from "../../../types/models/Sitting";

export interface AnswerProps {
  question: SittingQuestion;
  // a number for each point of the Question, 0 for one left empty
  given: number[];
  onChange: (given: number[]) => void;
}

// The four answers. Nothing is marked: the server alone knows which one is right. A click on
// the chosen answer takes the choice back.
const letters = ["A", "B", "C", "D"];

export default function ChoiceAnswer({ question, given, onChange }: AnswerProps) {
  const [chosen] = given;

  return (
    <div className="sitting-options">
      {(question.options ?? []).map((text, index) => {
        const option = index + 1;
        return (
          <button
            key={option}
            type="button"
            className={`quiz-play-item${chosen === option ? " chosen" : ""}`}
            aria-pressed={chosen === option}
            onClick={() => onChange([chosen === option ? 0 : option])}
          >
            <span className="quiz-play-letter" aria-hidden="true">
              {letters[index]}
            </span>
            <span className="quiz-play-item-text">{text}</span>
          </button>
        );
      })}
    </div>
  );
}
