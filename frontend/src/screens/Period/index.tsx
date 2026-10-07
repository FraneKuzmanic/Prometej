import { Link as RouterLink, useLocation, useNavigate, useParams, useSearchParams } from "react-router-dom";
import ReactQuill from "react-quill";
import "react-quill/dist/quill.snow.css";
import { useEffect, useMemo, useState } from "react";
import { RootState, useAppDispatch } from "../../store/store";
import {
  fetchPeriods,
  fetchPeriodContent,
  editPeriodContent,
} from "../../store/slices/periodSlice";
import { useSelector } from "react-redux";
import { Box, Button, Typography } from "@mui/material";
import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import EditOutlinedIcon from "@mui/icons-material/EditOutlined";
import ForumOutlinedIcon from "@mui/icons-material/ForumOutlined";
import MenuBookOutlinedIcon from "@mui/icons-material/MenuBookOutlined";
import QuizOutlinedIcon from "@mui/icons-material/QuizOutlined";
import "./styles.css";
import { PeriodContentEditRequest } from "../../types/models/Period";
import ROLE from "../../types/enums/Role";
import ContentsList from "../../components/ContentsList";
import { ContentsEntry, withHeadingIds } from "../../components/ContentsList/headings";
import { EmptyState, Page } from "../../components/Page";
import Discussion from "../Discussion";

// What the seeded texts are made of, and no more: a colour or a size chosen by hand would
// part a text from the look of the others.
const toolbarOptions = [
  [{ header: [1, 2, 3, 4, false] }],
  ["bold", "italic", "underline"],
  ["blockquote"],
  [{ list: "ordered" }, { list: "bullet" }],
  ["link", "image"],
  ["clean"],
];

// Wider than the toolbar: a text written before it was narrowed keeps what it has.
const formats = [
  "font",
  "header",
  "align",
  "bold",
  "italic",
  "underline",
  "strike",
  "color",
  "background",
  "script",
  "blockquote",
  "code-block",
  "list",
  "bullet",
  "indent",
  "direction",
  "size",
  "link",
  "image",
  "video",
];

const DISCUSSION_ID = "rasprava";
const discussionEntry: ContentsEntry = { id: DISCUSSION_ID, text: "Rasprava", level: 2 };

export default function Period() {
  const { id } = useParams<{ id: string }>();
  const { periods, periodsFailed, periodContent, periodContentFailed } = useSelector(
    (state: RootState) => state.period
  );
  const { user } = useSelector((state: RootState) => state.user);
  const dispatch = useAppDispatch();
  const navigate = useNavigate();
  const [value, setValue] = useState("");
  const [text, setText] = useState("");
  const [isEdit, setIsEdit] = useState(false);
  const { html, entries, title } = useMemo(() => {
    const parsed = withHeadingIds(text);
    // A text that opens with a heading of the first level is headed by it on the page too.
    const first = new DOMParser().parseFromString(text, "text/html").body.firstElementChild;
    return { ...parsed, title: first?.tagName === "H1" ? first.textContent?.trim() : undefined };
  }, [text]);
  const { hash, key } = useLocation();
  const [searchParams] = useSearchParams();
  const periodsLoaded = periods !== undefined;
  // Open when the address names the Discussion or one of its Topics.
  const [discussionOpen, setDiscussionOpen] = useState(
    () => hash === `#${DISCUSSION_ID}` || searchParams.has("topic")
  );

  // Fetched whenever the screen opens, not only once: the number of a Period's Quizzes can
  // have changed. One answer holds every Period, so going from one to another needs no more.
  useEffect(() => {
    dispatch(fetchPeriods());
  }, [dispatch]);

  useEffect(() => {
    if (id) dispatch(fetchPeriodContent(id));
  }, [dispatch, id]);

  useEffect(() => {
    if (periodContent) {
      setValue(periodContent.content);
      setText(periodContent.content);
    } else {
      setValue("");
      setText("");
    }
  }, [periodContent]);

  // A heading named in the address is scrolled to, once the text holding it is on the page:
  // a citation of the tutor's leads here. The key changes when the same address is opened
  // again, so a second click on one citation scrolls again.
  useEffect(() => {
    if (!hash || isEdit || !periodsLoaded) return;
    // Taken as written: a heading's id is plain letters, and decoding a mistyped address throws.
    const heading = document.getElementById(hash.slice(1));
    if (!heading) return;
    if (!heading.closest(".content") && heading.id !== DISCUSSION_ID) return;
    if (heading.id === DISCUSSION_ID) setDiscussionOpen(true);
    const reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    heading.scrollIntoView({ behavior: reducedMotion ? "auto" : "smooth", block: "start" });
  }, [hash, key, html, isEdit, periodsLoaded]);

  const cleanHTMLContent = (html: string): string => {
    return html
      .replace(/<p>&nbsp;<\/p>/g, "") // Remove empty paragraphs with a non-breaking space
      .replace(/<p><br><\/p>/g, "")
      .replace(/<h2><br><\/h2>/g, ""); // Remove empty paragraphs with a <br> tag
  };

  const submitContent = async (): Promise<void> => {
    const content = cleanHTMLContent(value);
    if (id) {
      const formData: PeriodContentEditRequest = {
        id: periodContent ? periodContent.id : 0,
        periodId: id,
        content: content,
      };
      dispatch(editPeriodContent(formData));
    }
    setIsEdit(false);
    setText(content);
    setValue(content);
  };

  const handleCancel = (): void => {
    setIsEdit(false);
    setValue(text);
  };

  const openDiscussion = () => {
    setDiscussionOpen(true);
    const reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    // After the section has opened: its height changes where the scroll has to end.
    requestAnimationFrame(() =>
      document
        .getElementById(DISCUSSION_ID)
        ?.scrollIntoView({ behavior: reducedMotion ? "auto" : "smooth", block: "start" })
    );
  };

  // Until the list is here the page cannot tell an empty Period from one that does not exist.
  if (!periods) {
    return periodsFailed ? (
      <Typography>Razdoblja se nisu učitala. Pokušajte ponovno.</Typography>
    ) : null;
  }
  const period = periods.find((period) => period.id === Number(id));
  if (!period) {
    return <Typography>Razdoblje ne postoji.</Typography>;
  }
  // Without this an Admin could take a lost connection for an empty Period and write over
  // the content that did not load.
  if (periodContentFailed) {
    return <Typography>Gradivo se nije učitalo. Pokušajte ponovno.</Typography>;
  }

  const isAdmin = user?.role === ROLE.Admin;

  if (isEdit) {
    return (
      <Page>
        <Box className="period-edit-bar">
          <Typography variant="h5" component="h1">
            Uređivanje gradiva: {period.name}
          </Typography>
          <Box className="period-edit-buttons">
            <Button onClick={handleCancel}>Odustani</Button>
            <Button variant="contained" onClick={() => submitContent()}>
              Spremi
            </Button>
          </Box>
        </Box>
        <Box className="period-editor">
          <ReactQuill
            modules={{ toolbar: toolbarOptions }}
            formats={formats}
            theme="snow"
            value={value}
            onChange={setValue}
          />
        </Box>
      </Page>
    );
  }

  const empty = periodContent === null && text === "";
  // The page's own heading already says the Period's name.
  const repeatsName = title === period.name;

  return (
    <Page>
      <Box component="header" className="period-hero">
        <Box className="period-hero-text">
          <RouterLink className="back-link" to="/learning">
            <ArrowBackIcon fontSize="small" /> Razdoblja
          </RouterLink>
          <Typography variant="h3" component="h1">
            {period.name}
          </Typography>
          <Typography className="period-hero-time">{period.timeFrame}</Typography>
          <Typography className="period-hero-description">{period.description}</Typography>
          <Box className="period-hero-actions">
            {period.quizCount > 0 && (
              <Button
                variant="contained"
                startIcon={<QuizOutlinedIcon />}
                onClick={() => navigate(`/quizzes?period=${period.id}`)}
              >
                Provjeri znanje
              </Button>
            )}
            <Button
              variant="outlined"
              startIcon={<ForumOutlinedIcon />}
              onClick={openDiscussion}
            >
              Rasprava
            </Button>
            {isAdmin && (
              <Button startIcon={<EditOutlinedIcon />} onClick={() => setIsEdit(true)}>
                Uredi gradivo
              </Button>
            )}
          </Box>
        </Box>
        {period.image && (
          <Box className="period-hero-image">
            <img src={`/${period.image}`} alt="" />
          </Box>
        )}
      </Box>
      <Box className={entries.length > 1 ? "period-layout" : undefined}>
        <ContentsList
          entries={entries.length > 1 ? [...entries, discussionEntry] : entries}
          onGoTo={(entryId) => entryId === DISCUSSION_ID && setDiscussionOpen(true)}
        />
        <Box className="period-main">
          {empty ? (
            <Box className="period-empty">
              <EmptyState
                icon={<MenuBookOutlinedIcon />}
                title="Za ovo razdoblje još nema gradiva."
                action={
                  isAdmin && (
                    <Button variant="contained" onClick={() => setIsEdit(true)}>
                      Napiši gradivo
                    </Button>
                  )
                }
              />
            </Box>
          ) : (
            <Box
              className={repeatsName ? "content content-titled" : "content"}
              dangerouslySetInnerHTML={{
                __html: html,
              }}
            />
          )}
          {period.quizCount > 0 && (
            <Box className="period-quizzes">
              <span className="icon-disc icon-disc-paper">
                <QuizOutlinedIcon />
              </span>
              <Box className="period-quizzes-text">
                <Typography variant="h5" component="h2">
                  Pročitano? Provjerite koliko ste zapamtili.
                </Typography>
                <Typography>Kvizova za ovo razdoblje: {period.quizCount}</Typography>
              </Box>
              <Button
                variant="contained"
                onClick={() => navigate(`/quizzes?period=${period.id}`)}
              >
                Otvori kvizove
              </Button>
            </Box>
          )}
          <Discussion period={period} open={discussionOpen} onToggle={setDiscussionOpen} />
        </Box>
      </Box>
    </Page>
  );
}
