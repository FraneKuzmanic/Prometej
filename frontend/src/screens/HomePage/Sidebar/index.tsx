import {
  List,
  ListItem,
  ListItemButton,
  ListItemIcon,
  ListItemText,
} from "@mui/material";
import LocalLibraryIcon from "@mui/icons-material/LocalLibrary";
import QuizIcon from "@mui/icons-material/Quiz";
import AssignmentIcon from "@mui/icons-material/Assignment";
import NoteAddIcon from "@mui/icons-material/NoteAdd";
import PersonIcon from "@mui/icons-material/Person";
import EmojiEventsIcon from "@mui/icons-material/EmojiEvents";
import GroupIcon from "@mui/icons-material/Group";
import "./styles.css";
import { useState } from "react";
import { useNavigate, useLocation } from "react-router-dom";
import ROLE from "../../../types/enums/Role";
import { UserViewModel } from "../../../types/models/User";

interface SidebarProps {
  toggle: boolean;
  user: UserViewModel | undefined;
  authenticated: boolean | undefined;
  setOpenJoinQuizDialog: (value: boolean) => void;
}

export default function Sidebar({
  toggle,
  user,
  authenticated,
  setOpenJoinQuizDialog,
}: SidebarProps) {
  const navigate = useNavigate();
  const location = useLocation();
  const [selectedItem, setSelectedItem] = useState(
    location.pathname.split("/")[1] || "learning"
  );

  const handleItemClick = (item: string) => {
    setSelectedItem(item);
    navigate(`/${item}`);
  };

  return (
    <List className="sidebar">
      <ListItem
        key={"learning"}
        sx={{ display: "block" }}
        className={`sidebar-item${
          selectedItem === "learning" ? "-selected" : ""
        }`}
      >
        <ListItemButton
          onClick={() => handleItemClick("learning")}
          sx={{
            minHeight: 48,
            justifyContent: toggle ? "initial" : "center",
            px: 2.5,
          }}
        >
          <ListItemIcon
            sx={{
              minWidth: 0,
              mr: toggle ? 3 : "auto",
              justifyContent: "center",
              color: selectedItem === "learning" ? "black" : "white",
            }}
          >
            <LocalLibraryIcon />
          </ListItemIcon>
          <ListItemText primary={"Učenje"} sx={{ opacity: toggle ? 1 : 0 }} />
        </ListItemButton>
      </ListItem>
      <ListItem
        key={"quizzes"}
        sx={{ display: "block" }}
        className={`sidebar-item${
          selectedItem === "quizzes" ? "-selected" : ""
        }`}
      >
        <ListItemButton
          onClick={() => handleItemClick("quizzes")}
          sx={{
            minHeight: 48,
            justifyContent: toggle ? "initial" : "center",
            px: 2.5,
          }}
        >
          <ListItemIcon
            sx={{
              minWidth: 0,
              mr: toggle ? 3 : "auto",
              justifyContent: "center",
              color: selectedItem === "quizzes" ? "black" : "white",
            }}
          >
            <QuizIcon />
          </ListItemIcon>
          <ListItemText primary={"Kvizovi"} sx={{ opacity: toggle ? 1 : 0 }} />
        </ListItemButton>
      </ListItem>
      {authenticated && (
        <ListItem
          key={"my-results"}
          sx={{ display: "block" }}
          className={`sidebar-item${
            selectedItem === "my-results" ? "-selected" : ""
          }`}
        >
          <ListItemButton
            onClick={() => handleItemClick("my-results")}
            sx={{
              minHeight: 48,
              justifyContent: toggle ? "initial" : "center",
              px: 2.5,
            }}
          >
            <ListItemIcon
              sx={{
                minWidth: 0,
                mr: toggle ? 3 : "auto",
                justifyContent: "center",
                color: selectedItem === "my-results" ? "black" : "white",
              }}
            >
              <EmojiEventsIcon />
            </ListItemIcon>
            <ListItemText
              primary={"Moji rezultati"}
              sx={{ opacity: toggle ? 1 : 0 }}
            />
          </ListItemButton>
        </ListItem>
      )}
      {authenticated &&
        (user?.role == ROLE.Teacher || user?.role === ROLE.Admin) && (
          <ListItem
            key={"my-quizzes"}
            sx={{ display: "block" }}
            className={`sidebar-item${
              selectedItem === "my-quizzes" ? "-selected" : ""
            }`}
          >
            <ListItemButton
              onClick={() => handleItemClick("my-quizzes")}
              sx={{
                minHeight: 48,
                justifyContent: toggle ? "initial" : "center",
                px: 2.5,
              }}
            >
              <ListItemIcon
                sx={{
                  minWidth: 0,
                  mr: toggle ? 3 : "auto",
                  justifyContent: "center",
                  color: selectedItem === "my-quizzes" ? "black" : "white",
                }}
              >
                <PersonIcon />
              </ListItemIcon>
              <ListItemText
                primary={"Moji kvizovi"}
                sx={{ opacity: toggle ? 1 : 0 }}
              />
            </ListItemButton>
          </ListItem>
        )}
      {authenticated &&
        (user?.role == ROLE.Teacher || user?.role === ROLE.Admin) && (
          <ListItem
            key={"make-quiz"}
            sx={{ display: "block" }}
            className={`sidebar-item${
              selectedItem === "make-quiz" ? "-selected" : ""
            }`}
          >
            <ListItemButton
              onClick={() => handleItemClick("make-quiz")}
              sx={{
                minHeight: 48,
                justifyContent: toggle ? "initial" : "center",
                px: 2.5,
              }}
            >
              <ListItemIcon
                sx={{
                  minWidth: 0,
                  mr: toggle ? 3 : "auto",
                  justifyContent: "center",
                  color: selectedItem === "make-quiz" ? "black" : "white",
                }}
              >
                <NoteAddIcon />
              </ListItemIcon>
              <ListItemText
                primary={"Napravi kviz"}
                sx={{ opacity: toggle ? 1 : 0 }}
              />
            </ListItemButton>
          </ListItem>
        )}
      {authenticated && user?.role === ROLE.Admin && (
        <ListItem
          key={"users"}
          sx={{ display: "block" }}
          className={`sidebar-item${
            selectedItem === "users" ? "-selected" : ""
          }`}
        >
          <ListItemButton
            onClick={() => handleItemClick("users")}
            sx={{
              minHeight: 48,
              justifyContent: toggle ? "initial" : "center",
              px: 2.5,
            }}
          >
            <ListItemIcon
              sx={{
                minWidth: 0,
                mr: toggle ? 3 : "auto",
                justifyContent: "center",
                color: selectedItem === "users" ? "black" : "white",
              }}
            >
              <GroupIcon />
            </ListItemIcon>
            <ListItemText
              primary={"Korisnici"}
              sx={{ opacity: toggle ? 1 : 0 }}
            />
          </ListItemButton>
        </ListItem>
      )}
      <ListItem
        key={"exam"}
        sx={{ display: "block" }}
        className={`sidebar-item${selectedItem === "exam" ? "-selected" : ""}`}
      >
        <ListItemButton
          onClick={() => setOpenJoinQuizDialog(true)}
          sx={{
            minHeight: 48,
            justifyContent: toggle ? "initial" : "center",
            px: 2.5,
          }}
        >
          <ListItemIcon
            sx={{
              minWidth: 0,
              mr: toggle ? 3 : "auto",
              justifyContent: "center",
              color: selectedItem === "exam" ? "black" : "white",
            }}
          >
            <AssignmentIcon />
          </ListItemIcon>
          <ListItemText
            primary={"Pridruži se kvizu"}
            sx={{ opacity: toggle ? 1 : 0 }}
          />
        </ListItemButton>
      </ListItem>
    </List>
  );
}
