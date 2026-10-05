import { Box, Button, IconButton, TextField, Typography } from "@mui/material";
import CloseIcon from "@mui/icons-material/Close";
import { EditorQuestion, MAX_ITEMS, MIN_ITEMS } from "../QuizEditor/questions";

interface OrderingFieldsProps {
  question: EditorQuestion;
  onChange: (updates: Partial<EditorQuestion>) => void;
}

// The items of an ordering Question. The order they are written in is the right one.
export default function OrderingFields({
  question,
  onChange,
}: OrderingFieldsProps) {
  const { items } = question;

  return (
    <Box className="matching-fields">
      <Typography className="ordering-note">
        Unesite pojmove pravim redoslijedom. Učenik će ih dobiti izmiješane.
      </Typography>
      {items.map((item, index) => (
        <Box className="matching-pair" key={index}>
          <Typography className="letter" component="span">
            {index + 1}
          </Typography>
          <TextField
            className="matching-input"
            size="small"
            placeholder="Pojam"
            inputProps={{ maxLength: 200, "aria-label": `Pojam ${index + 1}` }}
            value={item}
            onChange={(e) =>
              onChange({
                items: items.map((text, i) =>
                  i === index ? e.target.value : text
                ),
              })
            }
          />
          {/* An ordering Question keeps at least three items. */}
          {items.length > MIN_ITEMS ? (
            <IconButton
              aria-label={`Ukloni pojam ${index + 1}`}
              className="matching-remove"
              onClick={() =>
                onChange({ items: items.filter((_, i) => i !== index) })
              }
            >
              <CloseIcon />
            </IconButton>
          ) : (
            <span className="matching-remove" />
          )}
        </Box>
      ))}
      {items.length < MAX_ITEMS && (
        <Box className="matching-buttons">
          <Button
            variant="contained"
            onClick={() => onChange({ items: [...items, ""] })}
          >
            Dodaj pojam
          </Button>
        </Box>
      )}
    </Box>
  );
}
