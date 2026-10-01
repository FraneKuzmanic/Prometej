import { Box, Checkbox, Grid, TextField, Typography } from "@mui/material";
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
}

// The Correct Answer is the option's number, so editing an option's text cannot unmark it.
const options = [
  { letter: "A", field: "firstAnswer", number: 1 },
  { letter: "B", field: "secondAnswer", number: 2 },
  { letter: "C", field: "thirdAnswer", number: 3 },
  { letter: "D", field: "fourthAnswer", number: 4 },
] as const;

export default function QuestionContainer({
  currentQuestion,
  handleQuestionChange,
  selected,
}: QuestionContainerProps) {
  return (
    <Box className="question-wrapper">
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
    </Box>
  );
}
