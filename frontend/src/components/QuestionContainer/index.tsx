import { Box, TextField, Typography } from "@mui/material";
import { ReactNode } from "react";
import "./styles.css";
import { EditorQuestion } from "../QuizEditor/questions";
import ChoiceFields from "./ChoiceFields";
import MatchingFields from "./MatchingFields";
import OrderingFields from "./OrderingFields";

interface QuestionContainerProps {
  selected: number;
  currentQuestion?: EditorQuestion;
  handleQuestionChange: (
    questionNo: number,
    updates: Partial<EditorQuestion>
  ) => void;
  // Which kind the Question is: a choice while it can still change, a label once it cannot.
  kindField?: ReactNode;
  // The fields of the Question's Source Text, when it has one.
  sourceTextFields?: ReactNode;
}

// Both are optional. A Student can open the Hint before answering and sees Explore More
// after. A Teacher who meets the two fields for the first time is told what goes in each.
const explanations = [
  {
    field: "hintText",
    id: "question-hint",
    caption: "Pomoć prije odgovora (neobavezno)",
    about:
      "Učenik je može otvoriti prije nego što odgovori. Usmjerite ga, bez odavanja odgovora; ne utječe na bodove.",
  },
  {
    field: "exploreMore",
    id: "question-explore-more",
    caption: "Saznaj više nakon odgovora (neobavezno)",
    about:
      "Prikazuje se nakon odgovora, točnog ili netočnog. Objasnite zašto je odgovor točan ili uputite na djelo ili poglavlje gradiva.",
  },
] as const;

export default function QuestionContainer({
  currentQuestion,
  handleQuestionChange,
  selected,
  kindField,
  sourceTextFields,
}: QuestionContainerProps) {
  const onChange = (updates: Partial<EditorQuestion>) =>
    handleQuestionChange(selected, updates);

  return (
    <Box className="question-wrapper">
      {sourceTextFields}
      <Box className="question-card">
        <Box className="question-head">
          <Typography variant="h5" component="h2">
            Pitanje {selected + 1}
          </Typography>
          {kindField}
        </Box>
        <Box className="question-title">
          <TextField
            className="input-title"
            id="question-title"
            label="Unesite pitanje"
            fullWidth
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
        {currentQuestion?.type === "ordering" && (
          <OrderingFields question={currentQuestion} onChange={onChange} />
        )}
        <Box className="explanations">
          {explanations.map(({ field, id, caption, about }) => (
            <Box className="explanation" key={field}>
              <Typography className="explanation-caption" component="label" htmlFor={id}>
                {caption}
              </Typography>
              <TextField
                className="explanation-input"
                id={id}
                size="small"
                fullWidth
                multiline
                minRows={2}
                maxRows={5}
                inputProps={{ maxLength: 1000, "aria-label": caption }}
                helperText={about}
                value={currentQuestion ? currentQuestion[field] : ""}
                onChange={(e) => onChange({ [field]: e.target.value })}
              />
            </Box>
          ))}
        </Box>
      </Box>
    </Box>
  );
}
