import { Box, Button, IconButton, TextField, Typography } from "@mui/material";
import AddIcon from "@mui/icons-material/Add";
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
    <Box className="item-fields">
      <Typography className="ordering-note">
        Unesite pojmove pravim redoslijedom. Učenik će ih dobiti izmiješane.
      </Typography>
      {items.map((item, index) => (
        <Box className="item-row" key={index}>
          <Typography className="letter" component="span">
            {index + 1}
          </Typography>
          <TextField
            className="item-input"
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
              className="item-remove"
              onClick={() =>
                onChange({ items: items.filter((_, i) => i !== index) })
              }
            >
              <CloseIcon />
            </IconButton>
          ) : (
            <span className="item-remove" />
          )}
        </Box>
      ))}
      {items.length < MAX_ITEMS && (
        <Box className="item-buttons">
          <Button
            variant="outlined"
            size="small"
            startIcon={<AddIcon />}
            onClick={() => onChange({ items: [...items, ""] })}
          >
            Dodaj pojam
          </Button>
        </Box>
      )}
    </Box>
  );
}
