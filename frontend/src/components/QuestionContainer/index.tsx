import { Box, TextField, Typography } from "@mui/material";
import { ReactNode } from "react";
import "./styles.css";
import { EditorQuestion } from "../QuizEditor/questions";
import ChoiceFields from "./ChoiceFields";
import MatchingFields from "./MatchingFields";

interface QuestionContainerProps {
  selected: number;
  currentQuestion?: EditorQuestion;
  handleQuestionChange: (
    questionNo: number,
    updates: Partial<EditorQuestion>
  ) => void;
  // The fields of the Question's Source Text, when it has one.
  sourceTextFields?: ReactNode;
}

// Both are optional. A Student can open the Hint before answering and sees Explore More after.
const explanations = [
  {
    field: "hintText",
    id: "question-hint",
    caption: "Pomoć prije odgovora (neobavezno)",
  },
  {
    field: "exploreMore",
    id: "question-explore-more",
    caption: "Saznaj više nakon odgovora (neobavezno)",
  },
] as const;

export default function QuestionContainer({
  currentQuestion,
  handleQuestionChange,
  selected,
  sourceTextFields,
}: QuestionContainerProps) {
  const onChange = (updates: Partial<EditorQuestion>) =>
    handleQuestionChange(selected, updates);

  return (
    <Box className="question-wrapper">
      {sourceTextFields}
      <Box className="question-title">
        <TextField
          className="input-title"
          id="question-title"
          label="Unesite pitanje"
          multiline
          inputProps={{ maxLength: 500 }}
          value={currentQuestion ? currentQuestion.questionTitle : ""}
          onChange={(e) => onChange({ questionTitle: e.target.value })}
        />
      </Box>
      {/* The title, the Hint and Explore More are every Question's; the rest is its type's. */}
      {currentQuestion?.type === "choice" && (
        <ChoiceFields question={currentQuestion} onChange={onChange} />
      )}
      {currentQuestion?.type === "matching" && (
        <MatchingFields question={currentQuestion} onChange={onChange} />
      )}
      <Box className="explanations">
        {explanations.map(({ field, id, caption }) => (
          <Box className="explanation" key={field}>
            <Typography className="explanation-caption" component="span">
              {caption}
            </Typography>
            <TextField
              className="explanation-input"
              id={id}
              size="small"
              fullWidth
              multiline
              maxRows={2}
              inputProps={{ maxLength: 1000, "aria-label": caption }}
              value={currentQuestion ? currentQuestion[field] : ""}
              onChange={(e) => onChange({ [field]: e.target.value })}
            />
          </Box>
        ))}
      </Box>
    </Box>
  );
}
