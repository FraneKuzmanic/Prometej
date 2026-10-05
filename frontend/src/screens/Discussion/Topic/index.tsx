import { useEffect, useState } from "react";
import { useSelector } from "react-redux";
import { Link as RouterLink, useNavigate, useParams } from "react-router-dom";
import {
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
  Link,
  Paper,
  TextField,
  Typography,
} from "@mui/material";
import { RootState, useAppDispatch } from "../../../store/store";
import { fetchPeriods } from "../../../store/slices/periodSlice";
import {
  createReply,
  deleteReply,
  deleteTopic,
  fetchTopic,
} from "../../../store/slices/discussionSlice";
import PostAuthor from "../PostAuthor";
import PostError, { PostErrorKind } from "../PostError";
import SignInPrompt from "../SignInPrompt";
import "../styles.css";

type PendingDelete = { kind: "topic" } | { kind: "reply"; id: number };

// One Topic of a Period's Discussion with the Replies under it, the oldest first.
export default function Topic() {
  const { id, topicId: topicParam } = useParams<{
    id: string;
    topicId: string;
  }>();
  const topicId = Number(topicParam);
  const { periods } = useSelector((state: RootState) => state.period);
  const { authenticated } = useSelector((state: RootState) => state.user);
  const { topic: storedTopic, topicFailed, requestedTopicId } = useSelector(
    (state: RootState) => state.discussion
  );
  // The store may still hold the Topic opened before this one until the fetch below starts.
  const topic = requestedTopicId === topicId ? storedTopic : undefined;
  const dispatch = useAppDispatch();
  const navigate = useNavigate();
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
    if (!periods) dispatch(fetchPeriods());
  }, [dispatch, periods]);

  useEffect(() => {
    if (!Number.isInteger(topicId)) return;
    const request = dispatch(fetchTopic({ id: topicId }));
    // Leaving for another Topic drops this request, so its late answer cannot replace that Topic.
    return () => request.abort();
  }, [dispatch, topicId]);

  const discussionPath = `/learning/${id}/discussion`;
  const periodName = periods?.find((period) => period.id === Number(id))?.name;
  const backToDiscussion = (
    <Link component={RouterLink} to={discussionPath} underline="hover">
      ← Rasprava{periodName ? `: ${periodName}` : ""}
    </Link>
  );

  // A Topic has one Period: under another Period's address there is no such Topic.
  const missing =
    !Number.isInteger(topicId) ||
    topic === null ||
    (topic && topic.periodId !== Number(id));
  if (missing) {
    return (
      <Box className="discussion-wrapper">
        {backToDiscussion}
        <Typography className="discussion-actions">Tema ne postoji.</Typography>
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
          navigate(discussionPath);
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
    <Box className="discussion-wrapper">
      {backToDiscussion}
      {topicFailed && (
        <Typography className="discussion-actions">
          Tema se nije učitala. Pokušajte ponovno.
        </Typography>
      )}
      {topic && (
        <>
          <Typography variant="h4" className="discussion-title">
            {topic.title}
          </Typography>
          <Paper className="discussion-post">
            <Box className="discussion-post-header">
              <PostAuthor
                name={topic.authorName}
                role={topic.authorRole}
                date={topic.createdAt}
              />
              {topic.canDelete && (
                <Button
                  size="small"
                  color="error"
                  aria-label="Obriši temu"
                  onClick={() => openDialog({ kind: "topic" })}
                >
                  Obriši
                </Button>
              )}
            </Box>
            <Typography className="discussion-body">{topic.body}</Typography>
          </Paper>
          <Typography variant="h5" className="discussion-heading">
            Odgovori ({replyCount})
          </Typography>
          {replyCount === 0 && <Typography>Još nema odgovora.</Typography>}
          {topic.replies.map((reply) => (
            <Paper key={reply.id} className="discussion-post">
              <Box className="discussion-post-header">
                <PostAuthor
                  name={reply.authorName}
                  role={reply.authorRole}
                  date={reply.createdAt}
                />
                {reply.canDelete && (
                  <Button
                    size="small"
                    color="error"
                    aria-label="Obriši odgovor"
                    onClick={() => openDialog({ kind: "reply", id: reply.id })}
                  >
                    Obriši
                  </Button>
                )}
              </Box>
              <Typography className="discussion-body">{reply.body}</Typography>
            </Paper>
          ))}
          {authenticated === false && <SignInPrompt />}
          {authenticated && (
            <Box className="discussion-form">
              <TextField
                label="Vaš odgovor"
                multiline
                minRows={3}
                value={replyText}
                onChange={(e) => setReplyText(e.target.value)}
                inputProps={{ maxLength: 2000 }}
              />
              {postError && <PostError error={postError} />}
              <Box className="discussion-form-buttons">
                <Button
                  variant="contained"
                  sx={{ backgroundColor: "#553b08" }}
                  disabled={sending || !replyText.trim()}
                  onClick={handleReply}
                >
                  Odgovori
                </Button>
              </Box>
            </Box>
          )}
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
