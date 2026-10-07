import { useEffect, useState } from "react";
import { useSelector } from "react-redux";
import { useSearchParams } from "react-router-dom";
import { Box, Button, ButtonBase, Pagination, TextField, Typography } from "@mui/material";
import ExpandMoreIcon from "@mui/icons-material/ExpandMore";
import ForumOutlinedIcon from "@mui/icons-material/ForumOutlined";
import { RootState, useAppDispatch } from "../../store/store";
import { fetchPeriods } from "../../store/slices/periodSlice";
import { createTopic, fetchTopics } from "../../store/slices/discussionSlice";
import { Period } from "../../types/models/Period";
import PostAuthor from "./PostAuthor";
import Initials from "../../components/Initials";
import PostError, { PostErrorKind } from "./PostError";
import SignInPrompt from "./SignInPrompt";
import TopicThread from "./Topic";
import { formatDateTime, repliesLabel, topicsLabel } from "./format";
import "./styles.css";

interface DiscussionProps {
  period: Period;
  open: boolean;
  onToggle: (open: boolean) => void;
}

// The Discussion of one Period, at the foot of its page: a section that opens and closes,
// with its Topics, the one with the newest activity first. A Topic opens in place.
export default function Discussion({ period, open, onToggle }: DiscussionProps) {
  const { authenticated, user } = useSelector((state: RootState) => state.user);
  const { topicPage: storedPage, topicPageFailed, requestedPage } = useSelector(
    (state: RootState) => state.discussion
  );
  const dispatch = useAppDispatch();
  // The page and the open Topic are in the address, so both can be linked and reloaded.
  const [searchParams, setSearchParams] = useSearchParams();
  const pageParam = Number(searchParams.get("page"));
  // Anything the server would refuse is the first page.
  const page =
    Number.isInteger(pageParam) && pageParam >= 1 && pageParam <= 100000
      ? pageParam
      : 1;
  const topicParam = searchParams.get("topic");
  const openTopicId = topicParam === null ? undefined : Number(topicParam);
  const [formOpen, setFormOpen] = useState(false);
  const [title, setTitle] = useState("");
  const [body, setBody] = useState("");
  const [sending, setSending] = useState(false);
  const [postError, setPostError] = useState<PostErrorKind | undefined>();

  const periodId = period.id;
  const topicPage =
    requestedPage?.periodId === periodId && requestedPage?.page === page
      ? storedPage
      : undefined;

  useEffect(() => {
    if (!open) return;
    const request = dispatch(fetchTopics({ periodId, page }));
    // Leaving for another page drops this request, so its late answer cannot replace that page.
    return () => request.abort();
  }, [dispatch, periodId, page, open]);

  const setParams = (change: (params: URLSearchParams) => void) => {
    const params = new URLSearchParams(searchParams);
    change(params);
    setSearchParams(params);
  };

  const handlePageChange = (value: number) =>
    setParams((params) => {
      params.delete("topic");
      if (value > 1) params.set("page", String(value));
      else params.delete("page");
    });

  const toggleTopic = (id: number) =>
    setParams((params) => {
      if (openTopicId === id) params.delete("topic");
      else params.set("topic", String(id));
    });

  // The list and the Period's count are read again after a Topic was added or deleted.
  const reload = (toPage: number) => {
    dispatch(fetchTopics({ periodId, page: toPage }));
    dispatch(fetchPeriods());
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
    dispatch(createTopic({ periodId, data: { title, body } })).then((result) => {
      setSending(false);
      if (createTopic.fulfilled.match(result)) {
        closeForm();
        // A new Topic is the newest, so it is on the first page; it opens there.
        setParams((params) => {
          params.delete("page");
          params.set("topic", String(result.payload));
        });
        reload(1);
      } else {
        setPostError(result.payload === 429 ? "limit" : "other");
      }
    });
  };

  const handleTopicDeleted = () => {
    setParams((params) => params.delete("topic"));
    reload(page);
  };

  const total = topicPage?.total ?? period.topicCount;
  // A Topic named in the address that this page of the list does not hold: opened from a
  // link, or one that does not exist. It is shown above the list.
  const linkedTopic =
    openTopicId !== undefined &&
    topicPage &&
    !topicPage.topics.some((topic) => topic.id === openTopicId);

  return (
    <section className="discussion" id="rasprava" aria-label="Rasprava">
      <ButtonBase
        className="discussion-toggle"
        aria-expanded={open}
        aria-controls="rasprava-sadrzaj"
        onClick={() => onToggle(!open)}
      >
        <span className="icon-disc">
          <ForumOutlinedIcon />
        </span>
        <span className="discussion-toggle-text">
          <Typography variant="h5" component="h2">
            Rasprava
          </Typography>
          <span className="discussion-toggle-count">
            {total === 0
              ? "Još nema tema. Postavite prvo pitanje o ovom razdoblju."
              : `${total} ${topicsLabel(total)}`}
          </span>
        </span>
        <span className="discussion-toggle-action">
          {open ? "Zatvori" : "Otvori"}
          <ExpandMoreIcon className={open ? "discussion-chevron open" : "discussion-chevron"} />
        </span>
      </ButtonBase>
      {open && (
        <Box className="discussion-body" id="rasprava-sadrzaj">
          {authenticated === false && <SignInPrompt />}
          {authenticated && !formOpen && (
            <Box className="discussion-compose">
              <Initials name={user ? `${user.firstName} ${user.lastName}` : null} />
              <ButtonBase
                className="discussion-compose-field"
                onClick={() => setFormOpen(true)}
              >
                Nova tema: postavite pitanje ili podijelite zapažanje…
              </ButtonBase>
            </Box>
          )}
          {authenticated && formOpen && (
            <Box className="discussion-form">
              <TextField
                label="Naslov"
                autoFocus
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
              {postError && <PostError error={postError} />}
              <Box className="discussion-form-buttons">
                <Button onClick={closeForm}>Odustani</Button>
                <Button
                  variant="contained"
                  disabled={sending || !title.trim() || !body.trim()}
                  onClick={handleSubmit}
                >
                  Objavi
                </Button>
              </Box>
            </Box>
          )}
          {topicPageFailed && (
            <Typography className="discussion-note">
              Rasprava se nije učitala. Pokušajte ponovno.
            </Typography>
          )}
          {/* A page past the last one, after a Topic of the last page was deleted. */}
          {topicPage && topicPage.total > 0 && topicPage.topics.length === 0 && (
            <Typography className="discussion-note">Na ovoj stranici nema tema.</Typography>
          )}
          {linkedTopic && (
            <Box className="discussion-topic open">
              <TopicThread
                topicId={openTopicId}
                periodId={periodId}
                standalone
                onClose={() => toggleTopic(openTopicId)}
                onDeleted={handleTopicDeleted}
              />
            </Box>
          )}
          {topicPage?.topics.map((topic) => {
            const isOpen = topic.id === openTopicId;
            return (
              <Box
                key={topic.id}
                className={isOpen ? "discussion-topic open" : "discussion-topic"}
              >
                <ButtonBase
                  className="discussion-row"
                  aria-expanded={isOpen}
                  onClick={() => toggleTopic(topic.id)}
                >
                  <Initials name={topic.authorName} />
                  <span className="discussion-row-text">
                    <span className="discussion-row-title">{topic.title}</span>
                    <PostAuthor
                      name={topic.authorName}
                      role={topic.authorRole}
                      date={topic.createdAt}
                    />
                  </span>
                  <span className="discussion-row-meta">
                    {topic.replyCount} {repliesLabel(topic.replyCount)}
                    {topic.replyCount > 0 && (
                      <span className="discussion-row-last">
                        zadnji {formatDateTime(topic.lastActivityAt)}
                      </span>
                    )}
                  </span>
                  <ExpandMoreIcon
                    className={isOpen ? "discussion-chevron open" : "discussion-chevron"}
                  />
                </ButtonBase>
                {isOpen && (
                  <TopicThread
                    topicId={topic.id}
                    periodId={periodId}
                    onClose={() => toggleTopic(topic.id)}
                    onDeleted={handleTopicDeleted}
                  />
                )}
              </Box>
            );
          })}
          {topicPage && topicPage.total > topicPage.pageSize && (
            <Box className="discussion-pagination">
              <Pagination
                page={page}
                color="primary"
                count={Math.ceil(topicPage.total / topicPage.pageSize)}
                onChange={(_event, value: number) => handlePageChange(value)}
              />
            </Box>
          )}
        </Box>
      )}
    </section>
  );
}
