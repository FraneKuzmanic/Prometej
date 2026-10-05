import { useEffect, useState } from "react";
import { useSelector } from "react-redux";
import {
  Link as RouterLink,
  useNavigate,
  useParams,
  useSearchParams,
} from "react-router-dom";
import {
  Box,
  Button,
  Link,
  Pagination,
  Paper,
  TextField,
  Typography,
} from "@mui/material";
import { RootState, useAppDispatch } from "../../store/store";
import { fetchPeriods } from "../../store/slices/periodSlice";
import { createTopic, fetchTopics } from "../../store/slices/discussionSlice";
import PostAuthor from "./PostAuthor";
import SignInPrompt from "./SignInPrompt";
import { formatDateTime, repliesLabel } from "./format";
import "./styles.css";

// The Discussion of one Period: its Topics, the one with the newest activity first.
export default function Discussion() {
  const { id } = useParams<{ id: string }>();
  const { periods, periodsFailed } = useSelector(
    (state: RootState) => state.period
  );
  const { authenticated } = useSelector((state: RootState) => state.user);
  const { topicPage: storedPage, topicPageFailed, topicPageOf } = useSelector(
    (state: RootState) => state.discussion
  );
  const dispatch = useAppDispatch();
  const navigate = useNavigate();
  // The page is in the address, so a page of a Discussion can be linked and reloaded.
  const [searchParams, setSearchParams] = useSearchParams();
  const pageParam = Number(searchParams.get("page"));
  // Anything the server would refuse is the first page.
  const page =
    Number.isInteger(pageParam) && pageParam >= 1 && pageParam <= 100000
      ? pageParam
      : 1;
  const [formOpen, setFormOpen] = useState(false);
  const [title, setTitle] = useState("");
  const [body, setBody] = useState("");
  const [sending, setSending] = useState(false);
  // "limit": the server lets one User post only so often.
  const [postError, setPostError] = useState<"limit" | "other" | undefined>();

  const period = periods?.find((period) => period.id === Number(id));
  const periodId = period?.id;
  const topicPage =
    topicPageOf?.periodId === periodId && topicPageOf?.page === page
      ? storedPage
      : undefined;

  useEffect(() => {
    if (!periods) dispatch(fetchPeriods());
  }, [dispatch, periods]);

  useEffect(() => {
    if (periodId === undefined) return;
    const request = dispatch(fetchTopics({ periodId, page }));
    // Leaving for another page drops this request, so its late answer cannot replace that page.
    return () => request.abort();
  }, [dispatch, periodId, page]);

  // Until the list is here the page cannot tell which Period this is, or whether there is one.
  if (!periods) {
    return periodsFailed ? (
      <Typography>Razdoblja se nisu učitala. Pokušajte ponovno.</Typography>
    ) : null;
  }
  if (!period) {
    return <Typography>Razdoblje ne postoji.</Typography>;
  }

  const handlePageChange = (value: number) => {
    const params = new URLSearchParams(searchParams);
    if (value > 1) params.set("page", String(value));
    else params.delete("page");
    setSearchParams(params);
  };

  const closeForm = () => {
    setFormOpen(false);
    setTitle("");
    setBody("");
    setPostError(undefined);
  };

  const handleSubmit = () => {
    setSending(true);
    setPostError(undefined);
    dispatch(
      createTopic({ periodId: period.id, data: { title, body } })
    ).then((result) => {
      setSending(false);
      if (createTopic.fulfilled.match(result)) {
        navigate(`/learning/${period.id}/discussion/${result.payload}`);
      } else {
        setPostError(result.payload === 429 ? "limit" : "other");
      }
    });
  };

  return (
    <Box className="discussion-wrapper">
      <Link component={RouterLink} to={`/learning/${period.id}`} underline="hover">
        ← {period.name}
      </Link>
      <Typography variant="h3" className="discussion-title">
        Rasprava: {period.name}
      </Typography>
      {authenticated === false && <SignInPrompt />}
      {authenticated && !formOpen && (
        <Box className="discussion-actions">
          <Button
            variant="contained"
            sx={{ backgroundColor: "#553b08" }}
            onClick={() => setFormOpen(true)}
          >
            Nova tema
          </Button>
        </Box>
      )}
      {authenticated && formOpen && (
        <Box className="discussion-form">
          <TextField
            label="Naslov"
            value={title}
            onChange={(e) => setTitle(e.target.value)}
            inputProps={{ maxLength: 150 }}
          />
          <TextField
            label="Tekst"
            multiline
            minRows={4}
            value={body}
            onChange={(e) => setBody(e.target.value)}
            inputProps={{ maxLength: 2000 }}
          />
          {postError && (
            <Typography color="error" role="alert">
              {postError === "limit"
                ? "Pričekajte trenutak prije sljedeće objave."
                : "Objava nije spremljena. Pokušajte ponovno."}
            </Typography>
          )}
          <Box className="discussion-form-buttons">
            <Button
              variant="contained"
              sx={{ backgroundColor: "#553b08" }}
              disabled={sending || !title.trim() || !body.trim()}
              onClick={handleSubmit}
            >
              Objavi
            </Button>
            <Button onClick={closeForm}>Odustani</Button>
          </Box>
        </Box>
      )}
      {topicPageFailed && (
        <Typography>Rasprava se nije učitala. Pokušajte ponovno.</Typography>
      )}
      {topicPage && topicPage.total === 0 && (
        <Typography>Još nema tema. Postavite prvo pitanje.</Typography>
      )}
      {topicPage?.topics.map((topic) => (
        <Paper
          key={topic.id}
          className="discussion-row"
          component={RouterLink}
          to={`/learning/${period.id}/discussion/${topic.id}`}
        >
          <Typography className="discussion-row-title">{topic.title}</Typography>
          <PostAuthor
            name={topic.authorName}
            role={topic.authorRole}
            date={topic.createdAt}
          />
          <Typography className="discussion-row-meta">
            {topic.replyCount} {repliesLabel(topic.replyCount)}
            {topic.replyCount > 0 &&
              ` · zadnji odgovor ${formatDateTime(topic.lastActivityAt)}`}
          </Typography>
        </Paper>
      ))}
      {topicPage && topicPage.total > topicPage.pageSize && (
        <Box className="discussion-pagination">
          <Pagination
            page={page}
            count={Math.ceil(topicPage.total / topicPage.pageSize)}
            onChange={(_event, value: number) => handlePageChange(value)}
          />
        </Box>
      )}
    </Box>
  );
}
