import { KeyboardEvent, useEffect, useRef, useState } from "react";
import { useSelector } from "react-redux";
import { useNavigate } from "react-router-dom";
import {
  Box,
  Button,
  Chip,
  Drawer,
  IconButton,
  TextField,
  Typography,
  useMediaQuery,
} from "@mui/material";
import CloseIcon from "@mui/icons-material/Close";
import AddCommentOutlinedIcon from "@mui/icons-material/AddCommentOutlined";
import SendIcon from "@mui/icons-material/Send";
import { RootState, useAppDispatch } from "../../store/store";
import { askTutor, closeTutor, newConversation } from "../../store/slices/tutorSlice";
import { TutorCitation } from "../../types/models/Tutor";
import TutorMessage from "./TutorMessage";
import { TUTOR_AVATAR } from "./avatar";
import "./styles.css";

const PANEL_WIDTH = 400;
const MAX_QUESTION = 500;

const PERIOD_EXAMPLES = ["O čemu govori ovo razdoblje?", "Koja su djela iz ovog razdoblja?"];
const OTHER_EXAMPLES = ["Pronađi mi nešto o Dostojevskom", "U kojem je razdoblju pisao Marulić?"];

interface TutorProps {
  // The Period being read, when the panel is open on a Period's page.
  periodId: number | null;
  periodName: string | undefined;
}

export default function Tutor({ periodId, periodName }: TutorProps) {
  const { open, messages, pending, error } = useSelector((state: RootState) => state.tutor);
  const dispatch = useAppDispatch();
  const navigate = useNavigate();
  const narrow = useMediaQuery("(max-width:599.95px)");
  const [question, setQuestion] = useState("");
  const end = useRef<HTMLDivElement>(null);
  const field = useRef<HTMLTextAreaElement>(null);

  // The button that opened the panel is gone once it is open, so focus goes to the field.
  useEffect(() => {
    if (open) field.current?.focus();
  }, [open]);

  useEffect(() => {
    end.current?.scrollIntoView({ block: "end" });
  }, [messages.length, pending, error]);

  const ask = (text: string) => {
    const trimmed = text.trim();
    if (trimmed === "" || pending) return;
    dispatch(askTutor({ question: trimmed, periodId }));
    setQuestion("");
  };

  const askAgain = () => {
    const last = messages[messages.length - 1];
    if (last?.role === "user") {
      dispatch(askTutor({ question: last.content, periodId, retry: true }));
    }
  };

  const handleKeyDown = (event: KeyboardEvent) => {
    if (event.key === "Enter" && !event.shiftKey) {
      event.preventDefault();
      ask(question);
    }
  };

  const openCitation = (citation: TutorCitation) => {
    navigate(`/learning/${citation.periodId}#${citation.sectionId}`);
    // On a phone the panel covers the text the citation points to.
    if (narrow) dispatch(closeTutor());
  };

  return (
    // Persistent, not a dialog: the text beside it stays readable and a citation scrolls it.
    <Drawer
      anchor="right"
      variant="persistent"
      open={open}
      className="tutor-panel"
      sx={{
        width: open && !narrow ? PANEL_WIDTH : 0,
        flexShrink: 0,
        "& .MuiDrawer-paper": {
          width: narrow ? "100%" : PANEL_WIDTH,
          top: { xs: 56, sm: 64 },
          height: { xs: "calc(100% - 56px)", sm: "calc(100% - 64px)" },
        },
      }}
    >
      <Box
        component="section"
        aria-label="Prometej"
        className="tutor-layout"
        onKeyDown={(event) => {
          if (event.key === "Escape") dispatch(closeTutor());
        }}
      >
        <Box className="tutor-head">
          <img src={TUTOR_AVATAR} alt="" />
          <Box className="tutor-head-names">
            <Typography className="tutor-name">Prometej</Typography>
            <Typography className="tutor-context">{periodName ?? "Sva razdoblja"}</Typography>
          </Box>
          <Button
            size="small"
            color="inherit"
            className="tutor-new"
            startIcon={<AddCommentOutlinedIcon />}
            disabled={pending || messages.length === 0}
            onClick={() => dispatch(newConversation())}
          >
            Novi razgovor
          </Button>
          <IconButton aria-label="Zatvori" onClick={() => dispatch(closeTutor())}>
            <CloseIcon />
          </IconButton>
        </Box>

        <Box className="tutor-messages" aria-live="polite">
          {messages.length === 0 && (
            <Box className="tutor-empty">
              <Typography>
                Ja sam Prometej. Pitaj me o gradivu: odgovaram samo iz tekstova o razdobljima i
                pokažem ti gdje piše.
              </Typography>
              <Box className="tutor-examples">
                {(periodId === null ? OTHER_EXAMPLES : PERIOD_EXAMPLES).map((example) => (
                  <Chip key={example} label={example} variant="outlined" onClick={() => ask(example)} />
                ))}
              </Box>
            </Box>
          )}
          {messages.map((message, index) => (
            <TutorMessage key={index} message={message} onCitation={openCitation} />
          ))}
          {pending && (
            <Typography className="tutor-status" role="status">
              Prometej čita gradivo…
            </Typography>
          )}
          {error !== undefined && (
            <Box className="tutor-error" role="alert">
              <Typography color="error">
                {error === 429
                  ? "Previše pitanja u kratkom vremenu. Pokušaj ponovno za minutu."
                  : "Prometej trenutno nije dostupan. Pokušaj ponovno."}
              </Typography>
              {/* The question is still the last message; it is sent again, not typed again. */}
              <Button size="small" sx={{ color: "#553b08" }} onClick={askAgain}>
                Pokušaj ponovno
              </Button>
            </Box>
          )}
          <div ref={end} />
        </Box>

        <Box className="tutor-form">
          <TextField
            multiline
            maxRows={4}
            fullWidth
            size="small"
            placeholder="Postavi pitanje…"
            value={question}
            inputRef={field}
            onChange={(event) => setQuestion(event.target.value)}
            onKeyDown={handleKeyDown}
            inputProps={{ maxLength: MAX_QUESTION, "aria-label": "Pitanje" }}
          />
          <IconButton
            aria-label="Pošalji"
            disabled={pending || question.trim() === ""}
            onClick={() => ask(question)}
          >
            <SendIcon />
          </IconButton>
        </Box>
      </Box>
    </Drawer>
  );
}
