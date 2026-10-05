import { Box, Checkbox, Grid, TextField, Typography } from "@mui/material";
import { ReactNode } from "react";
import "./styles.css";
import { QuestionCreateRequest } from "../../types/models/Quiz";
import RadioButtonUncheckedIcon from "@mui/icons-material/RadioButtonUnchecked";
import CheckCircleIcon from "@mui/icons-material/CheckCircle";

interface QuestionContainerProps {
  selected: number;
  currentQuestion?: QuestionCreateRequest;
  handleQuestionChange: (
    questionNo: number,
    updates: Partial<QuestionCreateRequest>
  ) => void;
  // The fields of the Question's Source Text, when it has one.
  sourceTextFields?: ReactNode;
}

// The Correct Answer is the option's number, so editing an option's text cannot unmark it.
const options = [
  { letter: "A", field: "firstAnswer", number: 1 },
  { letter: "B", field: "secondAnswer", number: 2 },
  { letter: "C", field: "thirdAnswer", number: 3 },
  { letter: "D", field: "fourthAnswer", number: 4 },
] as const;

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
          onChange={(e) =>
            handleQuestionChange(selected, { questionTitle: e.target.value })
          }
        />
      </Box>
      <Grid
        className="answers-grid"
        container
        rowSpacing={4}
        columnSpacing={{ xs: 1, sm: 2, md: 3 }}
      >
        {options.map(({ letter, field, number }) => (
          <Grid className="answer" item xs={6} key={number}>
            <Typography className="letter" component="span">
              {letter}
            </Typography>
            <TextField
              className="input"
              id={`answer-${number}`}
              label="Unesite odgovor"
              inputProps={{ maxLength: 500 }}
              value={currentQuestion ? currentQuestion[field] : ""}
              onChange={(e) =>
                handleQuestionChange(selected, { [field]: e.target.value })
              }
              multiline
            />
            <Checkbox
              className="question-checkbox"
              inputProps={{ "aria-label": `Točan odgovor ${letter}` }}
              icon={<RadioButtonUncheckedIcon />}
              checkedIcon={<CheckCircleIcon />}
              checked={currentQuestion?.correctOption === number}
              onChange={(e) =>
                handleQuestionChange(selected, {
                  correctOption: e.target.checked ? number : 0,
                })
              }
            />
          </Grid>
        ))}
      </Grid>
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
              onChange={(e) =>
                handleQuestionChange(selected, { [field]: e.target.value })
              }
            />
          </Box>
        ))}
      </Box>
    </Box>
  );
}
