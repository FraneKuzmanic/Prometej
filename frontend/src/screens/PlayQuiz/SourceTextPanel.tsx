import { useState } from "react";
import { Typography, useMediaQuery } from "@mui/material";
import { SourceTextViewModel } from "../../types/models/Quiz";

// The passage the Question is asked about: beside it on a wide window, folded above it on a
// narrow one. Its body is the Creator's plain text, shown with its line breaks.
export default function SourceTextPanel({
  sourceText,
}: {
  sourceText: SourceTextViewModel;
}) {
  const wide = useMediaQuery("(min-width:1200px)");
  // Open for the first Question of the text, then as the Student leaves it.
  const [open, setOpen] = useState(true);

  const text = (
    <>
      <Typography variant="subtitle1" className="quiz-play-source-caption">
        {sourceText.caption}
      </Typography>
      <Typography className="quiz-play-source-body">{sourceText.body}</Typography>
    </>
  );

  return (
    <aside className="quiz-play-source" aria-label="Polazni tekst">
      {wide ? (
        text
      ) : (
        <details open={open} onToggle={(e) => setOpen(e.currentTarget.open)}>
          <summary>Polazni tekst</summary>
          {text}
        </details>
      )}
    </aside>
  );
}
