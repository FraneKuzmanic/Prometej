import { Box, Typography } from "@mui/material";
import { TutorMessage as Message } from "../../store/slices/tutorSlice";
import { TutorCitation } from "../../types/models/Tutor";

interface TutorMessageProps {
  message: Message;
  onCitation: (citation: TutorCitation) => void;
}

// Everything here is rendered as text: an answer is a language model's and a quote is
// the material's.
export default function TutorMessage({ message, onCitation }: TutorMessageProps) {
  const citations = message.citations ?? [];
  return (
    <Box className={`tutor-message tutor-message-${message.role}`}>
      <Typography className="tutor-text">{message.content}</Typography>
      {citations.length > 0 && (
        <Box className="tutor-citations">
          <Typography className="tutor-citations-title">Gdje to piše</Typography>
          {citations.map((citation, index) => (
            <Box key={index} className="tutor-citation">
              <button type="button" onClick={() => onCitation(citation)}>
                {citation.periodName} · {citation.sectionTitle}
              </button>
              <blockquote>{citation.quote}</blockquote>
            </Box>
          ))}
        </Box>
      )}
    </Box>
  );
}
