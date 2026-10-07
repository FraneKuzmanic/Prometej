import { Box, Button, TextField, Typography } from "@mui/material";
import AddIcon from "@mui/icons-material/Add";
import { SourceTextRequest } from "../../types/models/Quiz";

interface SourceTextFieldsProps {
  sourceText: SourceTextRequest;
  // "Tekst 1", "Tekst 2": its number among the Quiz's Source Texts
  number?: number;
  onChange: (updates: Partial<SourceTextRequest>) => void;
  // Left out once the text has as many Questions as it may have.
  onAddQuestion?: () => void;
}

const fields = [
  { field: "caption", id: "source-text-caption", caption: "Autor i naslov", maxLength: 200 },
  { field: "body", id: "source-text-body", caption: "Polazni tekst", maxLength: 8000 },
] as const;

// The passage the selected Question is asked about, shared by the Questions next to it.
export default function SourceTextFields({
  sourceText,
  number,
  onChange,
  onAddQuestion,
}: SourceTextFieldsProps) {
  return (
    <Box className="source-text-fields">
      <Box className="question-head">
        <Typography variant="h5" component="h2">
          Polazni tekst{number ? ` ${number}` : ""}
        </Typography>
        {onAddQuestion && (
          <Button
            variant="outlined"
            size="small"
            startIcon={<AddIcon />}
            className="source-text-add-question"
            onClick={onAddQuestion}
          >
            Dodaj pitanje uz tekst
          </Button>
        )}
      </Box>
      <Typography className="fields-note">
        Pjesma ili ulomak koji učenik vidi pokraj svakog pitanja o njemu.
      </Typography>
      {fields.map(({ field, id, caption, maxLength }) => (
        <Box className="explanation" key={field}>
          <Typography className="explanation-caption" component="label" htmlFor={id}>
            {caption}
          </Typography>
          <TextField
            className="explanation-input"
            id={id}
            size="small"
            fullWidth
            multiline={field === "body"}
            minRows={field === "body" ? 6 : undefined}
            maxRows={field === "body" ? 14 : undefined}
            inputProps={{ maxLength, "aria-label": caption }}
            value={sourceText[field]}
            onChange={(e) => onChange({ [field]: e.target.value })}
          />
        </Box>
      ))}
    </Box>
  );
}
