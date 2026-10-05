import { Box, Button, IconButton, TextField, Typography } from "@mui/material";
import CloseIcon from "@mui/icons-material/Close";
import {
  EditorQuestion,
  MAX_EXTRAS,
  MAX_PAIRS,
  MIN_PAIRS,
} from "../QuizEditor/questions";
import { MatchPair } from "../../types/models/Quiz";

interface MatchingFieldsProps {
  question: EditorQuestion;
  onChange: (updates: Partial<EditorQuestion>) => void;
}

// The pairs of a matching Question, and the extra right-hand options that go with nothing.
// The Student gets the right-hand side shuffled.
export default function MatchingFields({
  question,
  onChange,
}: MatchingFieldsProps) {
  const { pairs, extras } = question;

  const changePair = (index: number, updates: Partial<MatchPair>) =>
    onChange({
      pairs: pairs.map((pair, i) => (i === index ? { ...pair, ...updates } : pair)),
    });

  return (
    <Box className="matching-fields">
      {pairs.map((pair, index) => (
        <Box className="matching-pair" key={index}>
          <Typography className="letter" component="span">
            {index + 1}
          </Typography>
          <TextField
            className="matching-input"
            size="small"
            placeholder="Lijevi pojam"
            inputProps={{
              maxLength: 200,
              "aria-label": `Lijevi pojam ${index + 1}`,
            }}
            value={pair.left}
            onChange={(e) => changePair(index, { left: e.target.value })}
          />
          <TextField
            className="matching-input"
            size="small"
            placeholder="Njegov par"
            inputProps={{
              maxLength: 200,
              "aria-label": `Njegov par ${index + 1}`,
            }}
            value={pair.right}
            onChange={(e) => changePair(index, { right: e.target.value })}
          />
          {/* A matching Question keeps at least three pairs. */}
          {pairs.length > MIN_PAIRS ? (
            <IconButton
              aria-label={`Ukloni par ${index + 1}`}
              className="matching-remove"
              onClick={() =>
                onChange({ pairs: pairs.filter((_, i) => i !== index) })
              }
            >
              <CloseIcon />
            </IconButton>
          ) : (
            <span className="matching-remove" />
          )}
        </Box>
      ))}
      {extras.map((extra, index) => (
        <Box className="matching-pair" key={`extra-${index}`}>
          <span className="letter matching-no-letter" />
          <span className="matching-input" />
          <TextField
            className="matching-input"
            size="small"
            placeholder="Dodatni odgovor (neobavezno)"
            inputProps={{
              maxLength: 200,
              "aria-label": `Dodatni odgovor ${index + 1}`,
            }}
            value={extra}
            onChange={(e) =>
              onChange({
                extras: extras.map((text, i) =>
                  i === index ? e.target.value : text
                ),
              })
            }
          />
          <span className="matching-remove" />
        </Box>
      ))}
      <Box className="matching-buttons">
        {pairs.length < MAX_PAIRS && (
          <Button
            variant="contained"
            onClick={() =>
              onChange({ pairs: [...pairs, { left: "", right: "" }] })
            }
          >
            Dodaj par
          </Button>
        )}
        {extras.length < MAX_EXTRAS && (
          <Button
            variant="contained"
            onClick={() => onChange({ extras: [...extras, ""] })}
          >
            Dodaj dodatni odgovor
          </Button>
        )}
      </Box>
    </Box>
  );
}
