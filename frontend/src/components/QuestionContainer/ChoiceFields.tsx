import { Checkbox, Grid, TextField, Typography } from "@mui/material";
import RadioButtonUncheckedIcon from "@mui/icons-material/RadioButtonUnchecked";
import CheckCircleIcon from "@mui/icons-material/CheckCircle";
import { EditorQuestion } from "../QuizEditor/questions";

interface ChoiceFieldsProps {
  question: EditorQuestion;
  onChange: (updates: Partial<EditorQuestion>) => void;
}

// The Correct Answer is the option's number, so editing an option's text cannot unmark it.
const options = [
  { letter: "A", field: "firstAnswer", number: 1 },
  { letter: "B", field: "secondAnswer", number: 2 },
  { letter: "C", field: "thirdAnswer", number: 3 },
  { letter: "D", field: "fourthAnswer", number: 4 },
] as const;

export default function ChoiceFields({ question, onChange }: ChoiceFieldsProps) {
  return (
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
            value={question[field]}
            onChange={(e) => onChange({ [field]: e.target.value })}
            multiline
          />
          <Checkbox
            className="question-checkbox"
            inputProps={{ "aria-label": `Točan odgovor ${letter}` }}
            icon={<RadioButtonUncheckedIcon />}
            checkedIcon={<CheckCircleIcon />}
            checked={question.correctOption === number}
            onChange={(e) =>
              onChange({ correctOption: e.target.checked ? number : 0 })
            }
          />
        </Grid>
      ))}
    </Grid>
  );
}
