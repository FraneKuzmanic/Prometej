import { useEffect, useState } from "react";
import { useSelector } from "react-redux";
import {
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
  IconButton,
  TextField,
  Tooltip,
  Typography,
} from "@mui/material";
import CloseIcon from "@mui/icons-material/Close";
import DeleteOutlineIcon from "@mui/icons-material/DeleteOutline";
import { RootState, useAppDispatch } from "../../../store/store";
import {
  createReply,
  deleteReply,
  deleteTopic,
  fetchTopic,
} from "../../../store/slices/discussionSlice";
import PostAuthor from "../PostAuthor";
import Initials from "../../../components/Initials";
import PostError, { PostErrorKind } from "../PostError";
import SignInPrompt from "../SignInPrompt";
import { repliesLabel } from "../format";

type PendingDelete = { kind: "topic" } | { kind: "reply"; id: number };

interface TopicThreadProps {
  topicId: number;
  periodId: number;
  // Not under its row of the list (opened from a link): it shows its own title and author.
  standalone?: boolean;
  onClose: () => void;
  onDeleted: () => void;
}

// One Topic, open in its Period's Discussion: its text and the Replies under it, the oldest
// first, and the field to add one.
export default function TopicThread({
  topicId,
  periodId,
  standalone,
  onClose,
  onDeleted,
}: TopicThreadProps) {
  const { authenticated } = useSelector((state: RootState) => state.user);
  const { topic: storedTopic, topicFailed, requestedTopicId } = useSelector(
    (state: RootState) => state.discussion
  );
  // The store may still hold the Topic opened before this one until the fetch below starts.
  const topic = requestedTopicId === topicId ? storedTopic : undefined;
  const dispatch = useAppDispatch();
  const [replyText, setReplyText] = useState("");
  const [sending, setSending] = useState(false);
  const [postError, setPostError] = useState<PostErrorKind | undefined>();
  // Kept after the dialog closes, so its text does not change while it fades out.
  const [pending, setPending] = useState<PendingDelete | null>(null);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [deleting, setDeleting] = useState(false);
  // "conflict": the Topic got a Reply after this page was read.
  const [deleteError, setDeleteError] = useState<
    "conflict" | "other" | undefined
  >();

  useEffect(() => {
    if (!Number.isInteger(topicId)) return;
    const request = dispatch(fetchTopic({ id: topicId }));
    // Leaving for another Topic drops this request, so its late answer cannot replace that Topic.
    return () => request.abort();
  }, [dispatch, topicId]);

  // A Topic has one Period: under another Period's address there is no such Topic.
  const missing =
    !Number.isInteger(topicId) ||
    topic === null ||
    (topic && topic.periodId !== periodId);
  if (missing) {
    return (
      <Box className="discussion-thread discussion-thread-missing">
        <Typography>Tema ne postoji.</Typography>
        <Button size="small" onClick={onClose}>
          Zatvori
        </Button>
      </Box>
    );
  }

  const handleReply = () => {
    setSending(true);
    setPostError(undefined);
    dispatch(createReply({ topicId, data: { body: replyText } })).then((result) => {
      setSending(false);
      if (createReply.fulfilled.match(result)) {
        setReplyText("");
        // Read again: the server decides who may delete what, and a Reply changes that.
        dispatch(fetchTopic({ id: topicId, keep: true }));
      } else {
        setPostError(result.payload === 429 ? "limit" : "other");
      }
    });
  };

  const openDialog = (target: PendingDelete) => {
    setPending(target);
    setDeleteError(undefined);
    setDialogOpen(true);
  };

  const handleDelete = () => {
    if (!pending) return;
    setDeleting(true);
    if (pending.kind === "topic") {
      dispatch(deleteTopic(topicId)).then((result) => {
        setDeleting(false);
        if (deleteTopic.fulfilled.match(result)) {
          onDeleted();
        } else {
          setDeleteError(result.payload === 409 ? "conflict" : "other");
          dispatch(fetchTopic({ id: topicId, keep: true }));
        }
      });
    } else {
      dispatch(deleteReply(pending.id)).then((result) => {
        setDeleting(false);
        if (deleteReply.fulfilled.match(result)) {
          setDialogOpen(false);
        } else {
          setDeleteError("other");
        }
        dispatch(fetchTopic({ id: topicId, keep: true }));
      });
    }
  };

  const replyCount = topic?.replies.length ?? 0;

  return (
    <Box className="discussion-thread">
      {topicFailed && (
        <Typography className="discussion-note">
          Tema se nije učitala. Pokušajte ponovno.
        </Typography>
      )}
      {topic && (
        <>
          {standalone && (
            <Box className="discussion-thread-head">
              <Initials name={topic.authorName} />
              <span className="discussion-row-text">
                <span className="discussion-row-title">{topic.title}</span>
                <PostAuthor
                  name={topic.authorName}
                  role={topic.authorRole}
                  date={topic.createdAt}
                />
              </span>
              <Tooltip title="Zatvori temu">
                <IconButton aria-label="Zatvori temu" onClick={onClose}>
                  <CloseIcon />
                </IconButton>
              </Tooltip>
            </Box>
          )}
          <Box className="discussion-post discussion-post-first">
            <Typography className="discussion-text">{topic.body}</Typography>
            {topic.canDelete && (
              <Tooltip title="Obriši temu">
                <IconButton
                  size="small"
                  className="discussion-delete"
                  aria-label="Obriši temu"
                  onClick={() => openDialog({ kind: "topic" })}
                >
                  <DeleteOutlineIcon fontSize="small" />
                </IconButton>
              </Tooltip>
            )}
          </Box>
          <Typography className="discussion-replies-title" component="h3">
            {replyCount === 0
              ? "Još nema odgovora"
              : `${replyCount} ${repliesLabel(replyCount)}`}
          </Typography>
          <Box className="discussion-replies">
            {topic.replies.map((reply) => (
              <Box key={reply.id} className="discussion-reply">
                <Initials name={reply.authorName} small />
                <Box className="discussion-post">
                  <PostAuthor
                    name={reply.authorName}
                    role={reply.authorRole}
                    date={reply.createdAt}
                  />
                  <Typography className="discussion-text">{reply.body}</Typography>
                  {reply.canDelete && (
                    <Tooltip title="Obriši odgovor">
                      <IconButton
                        size="small"
                        className="discussion-delete"
                        aria-label="Obriši odgovor"
                        onClick={() => openDialog({ kind: "reply", id: reply.id })}
                      >
                        <DeleteOutlineIcon fontSize="small" />
                      </IconButton>
                    </Tooltip>
                  )}
                </Box>
              </Box>
            ))}
            {authenticated === false && <SignInPrompt />}
            {authenticated && (
              <Box className="discussion-form discussion-reply-form">
                <TextField
                  label="Vaš odgovor"
                  multiline
                  minRows={2}
                  value={replyText}
                  onChange={(e) => setReplyText(e.target.value)}
                  inputProps={{ maxLength: 2000 }}
                />
                {postError && <PostError error={postError} />}
                <Box className="discussion-form-buttons">
                  <Button
                    variant="contained"
                    disabled={sending || !replyText.trim()}
                    onClick={handleReply}
                  >
                    Odgovori
                  </Button>
                </Box>
              </Box>
            )}
          </Box>
        </>
      )}
      <Dialog open={dialogOpen} onClose={() => setDialogOpen(false)}>
        <DialogTitle>
          {pending?.kind === "reply" ? "Brisanje odgovora" : "Brisanje teme"}
        </DialogTitle>
        <DialogContent>
          <DialogContentText>
            {pending?.kind === "reply"
              ? "Odgovor će biti trajno obrisan."
              : replyCount > 0
                ? `Tema će biti trajno obrisana zajedno s odgovorima (${replyCount}).`
                : "Tema će biti trajno obrisana."}
          </DialogContentText>
          {deleteError && (
            <DialogContentText color="error" sx={{ marginTop: 2 }}>
              {deleteError === "conflict"
                ? "Tema je u međuvremenu dobila odgovor i više je ne možete obrisati."
                : "Brisanje nije uspjelo. Pokušajte ponovno."}
            </DialogContentText>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDialogOpen(false)}>Odustani</Button>
          <Button color="error" disabled={deleting} onClick={handleDelete}>
            Obriši
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}
