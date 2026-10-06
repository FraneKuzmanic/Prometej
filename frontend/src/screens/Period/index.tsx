import { useLocation, useNavigate, useParams } from "react-router-dom";
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
import {
  Box,
  Button,
  Container,
  SpeedDial,
  SpeedDialAction,
  SpeedDialIcon,
  Typography,
} from "@mui/material";
import BorderColorIcon from "@mui/icons-material/BorderColor";
import SaveIcon from "@mui/icons-material/Save";
import CancelIcon from "@mui/icons-material/Cancel";
import "./styles.css";
import "react-quill/dist/quill.snow.css";
import { PeriodContentEditRequest } from "../../types/models/Period";
import ROLE from "../../types/enums/Role";
import ContentsList from "../../components/ContentsList";
import { withHeadingIds } from "../../components/ContentsList/headings";

const toolbarOptions = [
  ["bold", "italic", "underline", "strike"], // toggled buttons
  ["blockquote", "code-block"],
  ["link", "image", "video", "formula"],

  [{ header: 1 }, { header: 2 }], // custom button values
  [{ list: "ordered" }, { list: "bullet" }, { list: "check" }],
  [{ script: "sub" }, { script: "super" }], // superscript/subscript
  [{ indent: "-1" }, { indent: "+1" }], // outdent/indent
  [{ direction: "rtl" }], // text direction

  [{ size: ["small", false, "large", "huge"] }], // custom dropdown
  [{ header: [1, 2, 3, 4, 5, 6, false] }],

  [{ color: [] }, { background: [] }], // dropdown with defaults from theme
  [{ align: [] }],

  ["clean"], // remove formatting button
];

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
  const { html, entries } = useMemo(() => withHeadingIds(text), [text]);
  const { hash, key } = useLocation();
  const periodsLoaded = periods !== undefined;

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
    if (!heading?.closest(".content")) return;
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

  return (
    <Box className="content-wrapper">
      {isEdit ? (
        <>
          <ReactQuill
            modules={{ toolbar: toolbarOptions }}
            formats={formats}
            theme="snow"
            value={value}
            onChange={setValue}
          />
          <SpeedDial
            ariaLabel="SpeedDial basic example"
            sx={{
              position: "fixed",
              bottom: 16,
              right: 16,
            }}
            className="speed-dial"
            icon={<SpeedDialIcon />}
          >
            <SpeedDialAction
              key={"Spremi"}
              icon={<SaveIcon />}
              tooltipTitle={"Spremi"}
              onClick={() => submitContent()}
            />
            <SpeedDialAction
              key={"Odustani"}
              icon={<CancelIcon />}
              tooltipTitle={"Odustani"}
              onClick={() => handleCancel()}
            />
          </SpeedDial>
        </>
      ) : (
        <Container>
          {user?.role === ROLE.Admin && (
            <SpeedDial
              ariaLabel="SpeedDial basic example"
              sx={{
                position: "fixed",
                bottom: 16,
                right: 16,
              }}
              className="speed-dial"
              icon={<SpeedDialIcon />}
            >
              <SpeedDialAction
                key={"Uredi"}
                icon={<BorderColorIcon />}
                tooltipTitle={"Uredi"}
                onClick={() => setIsEdit(true)}
              />
            </SpeedDial>
          )}
          <Box className="period-actions">
            <Button
              variant="outlined"
              sx={{ color: "#553b08", borderColor: "#553b08" }}
              onClick={() => navigate(`/learning/${period.id}/discussion`)}
            >
              Rasprava ({period.topicCount})
            </Button>
          </Box>
          {periodContent === null && text === "" ? (
            <Typography>Za ovo razdoblje još nema gradiva.</Typography>
          ) : (
            <Box className={entries.length > 1 ? "period-layout" : undefined}>
              <ContentsList entries={entries} />
              <Box
                className="content"
                dangerouslySetInnerHTML={{
                  __html: html,
                }}
              />
            </Box>
          )}
          {period.quizCount > 0 && (
            <Box className="period-quizzes">
              <Button
                variant="contained"
                style={{ backgroundColor: "#553b08" }}
                onClick={() => navigate(`/quizzes?period=${period.id}`)}
              >
                Provjeri znanje
              </Button>
              <Typography>Kvizova za ovo razdoblje: {period.quizCount}</Typography>
            </Box>
          )}
        </Container>
      )}
    </Box>
  );
}
