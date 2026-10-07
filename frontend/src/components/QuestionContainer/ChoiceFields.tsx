import { Box, Checkbox, TextField, Tooltip, Typography } from "@mui/material";
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
    <>
      <Typography className="fields-note">
        Upišite četiri različita odgovora i označite točan.
      </Typography>
      <Box className="answers-grid">
        {options.map(({ letter, field, number }) => {
          const correct = question.correctOption === number;
          return (
            <Box className={correct ? "answer correct" : "answer"} key={number}>
              <Typography className="letter" component="span">
                {letter}
              </Typography>
              <TextField
                className="input"
                id={`answer-${number}`}
                placeholder="Unesite odgovor"
                inputProps={{ maxLength: 500, "aria-label": `Odgovor ${letter}` }}
                value={question[field]}
                onChange={(e) => onChange({ [field]: e.target.value })}
                multiline
              />
              <Tooltip title={correct ? "Točan odgovor" : "Označi kao točan"}>
                <Checkbox
                  className="question-checkbox"
                  inputProps={{ "aria-label": `Točan odgovor ${letter}` }}
                  icon={<RadioButtonUncheckedIcon />}
                  checkedIcon={<CheckCircleIcon />}
                  checked={correct}
                  onChange={(e) =>
                    onChange({ correctOption: e.target.checked ? number : 0 })
                  }
                />
              </Tooltip>
            </Box>
          );
        })}
      </Box>
    </>
  );
}
