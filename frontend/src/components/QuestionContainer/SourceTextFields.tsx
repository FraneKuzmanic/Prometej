import { Box, Button, TextField, Typography } from "@mui/material";
import { SourceTextRequest } from "../../types/models/Quiz";

interface SourceTextFieldsProps {
  sourceText: SourceTextRequest;
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
  onChange,
  onAddQuestion,
}: SourceTextFieldsProps) {
  return (
    <Box className="source-text-fields">
      {fields.map(({ field, id, caption, maxLength }) => (
        <Box className="explanation" key={field}>
          <Typography className="explanation-caption" component="span">
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
      {onAddQuestion && (
        <Button
          variant="contained"
          className="source-text-add-question"
          onClick={onAddQuestion}
        >
          Dodaj pitanje uz tekst
        </Button>
      )}
    </Box>
  );
}
