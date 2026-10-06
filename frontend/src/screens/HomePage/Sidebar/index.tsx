import {
  List,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  Tooltip,
} from "@mui/material";
import AutoStoriesOutlinedIcon from "@mui/icons-material/AutoStoriesOutlined";
import SchoolOutlinedIcon from "@mui/icons-material/SchoolOutlined";
import QuizOutlinedIcon from "@mui/icons-material/QuizOutlined";
import EmojiEventsOutlinedIcon from "@mui/icons-material/EmojiEventsOutlined";
import KeyOutlinedIcon from "@mui/icons-material/KeyOutlined";
import CoPresentOutlinedIcon from "@mui/icons-material/CoPresentOutlined";
import CollectionsBookmarkOutlinedIcon from "@mui/icons-material/CollectionsBookmarkOutlined";
import AddCircleOutlineIcon from "@mui/icons-material/AddCircleOutline";
import GroupOutlinedIcon from "@mui/icons-material/GroupOutlined";
import "./styles.css";
import { ReactNode } from "react";
import { useNavigate, useLocation } from "react-router-dom";
import ROLE from "../../../types/enums/Role";
import { UserViewModel } from "../../../types/models/User";

interface SidebarProps {
  toggle: boolean;
  user: UserViewModel | undefined;
  authenticated: boolean | undefined;
  setOpenJoinQuizDialog: (value: boolean) => void;
}

interface NavItem {
  label: string;
  icon: ReactNode;
  // Where the item leads. "Pridruži se kvizu" has none: it opens a dialog.
  path?: string;
  // Other screens that belong to the item, so it stays marked on them.
  alsoOn?: string[];
  onClick?: () => void;
}

interface NavGroup {
  label: string;
  icon: ReactNode;
  items: NavItem[];
}

export default function Sidebar({
  toggle,
  user,
  authenticated,
  setOpenJoinQuizDialog,
}: SidebarProps) {
  const navigate = useNavigate();
  const { pathname } = useLocation();
  const isCreator =
    authenticated && (user?.role === ROLE.Teacher || user?.role === ROLE.Admin);

  const periods: NavItem = {
    label: "Razdoblja",
    icon: <AutoStoriesOutlinedIcon />,
    path: "/learning",
    alsoOn: ["/search"],
  };

  const groups: NavGroup[] = [
    {
      label: "Učionica",
      icon: <SchoolOutlinedIcon />,
      items: [
        { label: "Kvizovi", icon: <QuizOutlinedIcon />, path: "/quizzes" },
        ...(authenticated
          ? [
              {
                label: "Moji rezultati",
                icon: <EmojiEventsOutlinedIcon />,
                path: "/my-results",
              },
            ]
          : []),
        {
          label: "Pridruži se kvizu",
          icon: <KeyOutlinedIcon />,
          onClick: () => setOpenJoinQuizDialog(true),
        },
      ],
    },
  ];

  if (isCreator) {
    groups.push({
      label: "Nastava",
      icon: <CoPresentOutlinedIcon />,
      items: [
        {
          label: "Moji kvizovi",
          icon: <CollectionsBookmarkOutlinedIcon />,
          path: "/my-quizzes",
          alsoOn: ["/quiz-details"],
        },
        { label: "Novi kviz", icon: <AddCircleOutlineIcon />, path: "/make-quiz" },
        ...(user?.role === ROLE.Admin
          ? [{ label: "Korisnici", icon: <GroupOutlinedIcon />, path: "/users" }]
          : []),
      ],
    });
  }

  // The address says which item is marked, so a link inside a page moves the mark too.
  const isOn = (item: NavItem) =>
    [item.path, ...(item.alsoOn ?? [])].some(
      (path) => path && (pathname === path || pathname.startsWith(`${path}/`))
    );

  const open = (item: NavItem) =>
    item.onClick ? item.onClick() : navigate(item.path!);

  const row = (item: NavItem) => {
    const selected = isOn(item);
    return (
      <li key={item.label}>
        {/* Collapsed, only the icon is seen, so the name is its tooltip. */}
        <Tooltip title={toggle ? "" : item.label} placement="right">
          <ListItemButton
            className="sidebar-row"
            selected={selected}
            aria-current={selected ? "page" : undefined}
            onClick={() => open(item)}
          >
            <ListItemIcon className="sidebar-icon">{item.icon}</ListItemIcon>
            <ListItemText className="sidebar-label" primary={item.label} />
          </ListItemButton>
        </Tooltip>
      </li>
    );
  };

  return (
    <nav
      className={`sidebar${toggle ? "" : " sidebar-collapsed"}`}
      aria-label="Glavna navigacija"
    >
      <List disablePadding>{row(periods)}</List>
      {groups.map((group) => (
        <div className="sidebar-group" key={group.label}>
          {/* A group's name opens its first item. */}
          <ListItemButton
            className="sidebar-row sidebar-group-name"
            onClick={() => open(group.items[0])}
          >
            <ListItemIcon className="sidebar-icon">{group.icon}</ListItemIcon>
            <ListItemText className="sidebar-label" primary={group.label} />
          </ListItemButton>
          <List disablePadding className="sidebar-group-items">
            {group.items.map(row)}
          </List>
        </div>
      ))}
    </nav>
  );
}
