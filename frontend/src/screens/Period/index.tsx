import { useParams } from "react-router-dom";
import ReactQuill from "react-quill";
import "react-quill/dist/quill.snow.css";
import { useEffect, useState } from "react";
import { RootState, useAppDispatch } from "../../store/store";
import {
  fetchPeriodContent,
  editPeriodContent,
} from "../../store/slices/periodSlice";
import { useSelector } from "react-redux";
import {
  Box,
  Container,
  Drawer,
  SpeedDial,
  SpeedDialAction,
  SpeedDialIcon,
} from "@mui/material";
import BorderColorIcon from "@mui/icons-material/BorderColor";
import SaveIcon from "@mui/icons-material/Save";
import CancelIcon from "@mui/icons-material/Cancel";
import "./styles.css";
import "react-quill/dist/quill.snow.css";
import { PeriodContentEditRequest } from "../../types/models/Period";
import ForumIcon from "@mui/icons-material/Forum";
import DiscussionGroup from "../DiscussionGroup";

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
  const { periodContent } = useSelector((state: RootState) => state.period);
  const dispatch = useAppDispatch();
  const [value, setValue] = useState("");
  const [text, setText] = useState("");
  const [isEdit, setIsEdit] = useState(false);
  const [isComment, setIsComment] = useState(false);

  useEffect(() => {
    if (id) dispatch(fetchPeriodContent(id));
  }, []);

  useEffect(() => {
    if (periodContent) {
      setValue(periodContent.content);
      setText(periodContent.content);
    } else {
      setValue("");
      setText("");
    }
  }, [periodContent]);

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
            <SpeedDialAction
              key={"Komentiraj"}
              icon={<ForumIcon />}
              tooltipTitle={"Komentiraj"}
              onClick={() => setIsComment(true)}
            />
          </SpeedDial>
          <Box
            className="content"
            dangerouslySetInnerHTML={{
              __html: text,
            }}
          />
        </Container>
      )}
      {isComment && (
        <Drawer
          onClose={() => setIsComment(false)}
          open={isComment}
          anchor="left"
        >
          <DiscussionGroup />
        </Drawer>
      )}
    </Box>
  );
}
