import {
  Avatar,
  Box,
  Divider,
  IconButton,
  ListItemIcon,
  ListSubheader,
  Menu,
  MenuItem,
  Toolbar,
  Typography,
} from "@mui/material";
import { AppBar, SearchField } from "../index.styled";
import SearchIcon from "@mui/icons-material/Search";
import CloseIcon from "@mui/icons-material/Close";
import AccountCircleOutlinedIcon from "@mui/icons-material/AccountCircleOutlined";
import PersonOutlineIcon from "@mui/icons-material/PersonOutline";
import LogoutIcon from "@mui/icons-material/Logout";
import LoginIcon from "@mui/icons-material/Login";
import PersonAddAltIcon from "@mui/icons-material/PersonAddAlt";
import { useNavigate } from "react-router-dom";
import { useSelector } from "react-redux";
import React from "react";
import { RootState, useAppDispatch } from "../../../store/store";
import { attemptLogout } from "../../../store/slices/userSlice";
import { useLocation } from "react-router-dom";
import { roleLabels } from "../../../types/enums/Role";

interface HeaderProps {
  toggle: boolean;
}

export default function Header({ toggle }: HeaderProps) {
  const [anchorEl, setAnchorEl] = React.useState<null | HTMLElement>(null);
  const { authenticated, user } = useSelector((state: RootState) => state.user);
  const [search, setSearch] = React.useState<string>("");
  const searchInput = React.useRef<HTMLInputElement>(null);
  const navigate = useNavigate();
  const location = useLocation();
  const pathname = location.pathname;
  const dispatch = useAppDispatch();

  // The field searches the material where the material is read and the public Quizzes on
  // their list. Every other screen has nothing it could search, and no field.
  const searchesMaterial =
    pathname.startsWith("/learning") || pathname.startsWith("/search");
  const searchesQuizzes = pathname === "/quizzes";
  const searchLabel = searchesMaterial ? "Pretraži gradivo" : "Pretraži kvizove";

  // The address holds the query, so the field shows it after a reload or a link as well.
  React.useEffect(() => {
    setSearch(new URLSearchParams(location.search).get("q") ?? "");
  }, [pathname, location.search]);

  const handleOpenUserMenu = (event: React.MouseEvent<HTMLElement>) => {
    setAnchorEl(event.currentTarget);
  };

  const handleCloseUserMenu = () => {
    setAnchorEl(null);
  };

  const openFromMenu = (path: string) => {
    handleCloseUserMenu();
    navigate(path);
  };

  const searchQuizzes = (query: string) => {
    // The quiz list reads its query and its Period from the address.
    const params = new URLSearchParams(location.search);
    if (query) params.set("q", query);
    else params.delete("q");
    navigate({ pathname: "/quizzes", search: params.toString() });
  };

  const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === "Enter") {
      const query = search.trim();
      if (searchesMaterial) {
        // The search screen reads the query from the address and fetches the results.
        navigate(`/search?q=${encodeURIComponent(query)}`);
      } else {
        searchQuizzes(query);
      }
    }
  };

  const clearSearch = () => {
    setSearch("");
    searchInput.current?.focus();
    // On the quiz list the query is a filter: clearing the field lifts it.
    if (searchesQuizzes && new URLSearchParams(location.search).has("q")) {
      searchQuizzes("");
    }
  };

  const handleLogout = (): void => {
    handleCloseUserMenu();
    // A full load, not a route change: the app starts over as a visitor's, with nothing of
    // the signed-out User's left in the store or on the screen.
    dispatch(attemptLogout()).then(() => window.location.assign("/learning"));
  };

  const signedIn = authenticated && user !== undefined;

  const userMenu = () => (
    <Menu
      id="menu-appbar"
      anchorEl={anchorEl}
      anchorOrigin={{ vertical: "bottom", horizontal: "right" }}
      transformOrigin={{ vertical: "top", horizontal: "right" }}
      open={Boolean(anchorEl)}
      onClose={handleCloseUserMenu}
      slotProps={{ paper: { sx: { minWidth: 232 } } }}
    >
      {signedIn
        ? [
            // A subheader, so the keyboard starts on the first action, not on the name.
            <ListSubheader
              key="who"
              sx={{ px: 1.25, pt: 0.75, pb: 0.5, lineHeight: 1.35, backgroundColor: "transparent" }}
            >
              <Typography sx={{ fontWeight: 600, color: "text.primary" }} noWrap>
                {user.firstName} {user.lastName}
              </Typography>
              <Typography sx={{ fontSize: "0.85rem", color: "text.secondary" }}>
                {roleLabels[user.role]}
              </Typography>
            </ListSubheader>,
            <Divider key="rule" sx={{ my: 0.75 }} />,
            <MenuItem key="account" onClick={() => openFromMenu("/account")}>
              <ListItemIcon>
                <PersonOutlineIcon fontSize="small" />
              </ListItemIcon>
              Moj račun
            </MenuItem>,
            <MenuItem key="logout" onClick={handleLogout}>
              <ListItemIcon>
                <LogoutIcon fontSize="small" />
              </ListItemIcon>
              Odjavi se
            </MenuItem>,
          ]
        : [
            <MenuItem key="login" onClick={() => openFromMenu("/login")}>
              <ListItemIcon>
                <LoginIcon fontSize="small" />
              </ListItemIcon>
              Prijavi se
            </MenuItem>,
            <MenuItem key="register" onClick={() => openFromMenu("/register")}>
              <ListItemIcon>
                <PersonAddAltIcon fontSize="small" />
              </ListItemIcon>
              Registriraj se
            </MenuItem>,
          ]}
    </Menu>
  );

  return (
    <AppBar position="fixed" open={toggle}>
      <Toolbar sx={{ gap: 2 }}>
        {(searchesMaterial || searchesQuizzes) && (
          <SearchField
            placeholder={`${searchLabel}…`}
            inputRef={searchInput}
            inputProps={{ "aria-label": searchLabel, maxLength: 100 }}
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            onKeyDown={handleKeyDown}
            startAdornment={<SearchIcon />}
            endAdornment={
              search !== "" && (
                <IconButton
                  size="small"
                  color="inherit"
                  aria-label="Obriši upit"
                  onClick={clearSearch}
                >
                  <CloseIcon fontSize="small" />
                </IconButton>
              )
            }
          />
        )}
        <Box sx={{ marginLeft: "auto" }}>
          <IconButton
            edge="end"
            aria-label="Korisnički izbornik"
            aria-controls="menu-appbar"
            aria-haspopup="true"
            onClick={handleOpenUserMenu}
            color="inherit"
          >
            {signedIn ? (
              <Avatar
                sx={{
                  width: 34,
                  height: 34,
                  fontSize: "0.875rem",
                  fontWeight: 600,
                  color: "primary.main",
                  backgroundColor: "#e9e5cd",
                }}
              >
                {user.firstName.charAt(0)}
                {user.lastName.charAt(0)}
              </Avatar>
            ) : (
              <AccountCircleOutlinedIcon sx={{ fontSize: 30 }} />
            )}
          </IconButton>
          {anchorEl && userMenu()}
        </Box>
      </Toolbar>
    </AppBar>
  );
}
