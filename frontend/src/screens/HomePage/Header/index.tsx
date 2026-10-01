import {
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
  IconButton,
  Menu,
  MenuItem,
  Toolbar,
} from "@mui/material";
import {
  AppBar,
  Search,
  SearchIconWrapper,
  StyledInputBase,
} from "../index.styled";
import MenuIcon from "@mui/icons-material/Menu";
import SearchIcon from "@mui/icons-material/Search";
import { AccountCircle } from "@mui/icons-material";
import { useNavigate } from "react-router-dom";
import { useSelector } from "react-redux";
import React from "react";
import { RootState, useAppDispatch } from "../../../store/store";
import {
  attemptLogout,
  deleteCurrentUser,
} from "../../../store/slices/userSlice";
import { useLocation } from "react-router-dom";
import { searchQuizzes } from "../../../store/slices/quizSlice";
import { searchPeriodContent } from "../../../store/slices/periodSlice";

interface HeaderProps {
  toggle: boolean;
  toggleSidebar: () => void;
}

export default function Header({ toggle, toggleSidebar }: HeaderProps) {
  const [anchorEl, setAnchorEl] = React.useState<null | HTMLElement>(null);
  const { authenticated } = useSelector((state: RootState) => state.user);
  const [search, setSearch] = React.useState<string>("");
  const [deleteDialogOpen, setDeleteDialogOpen] = React.useState(false);
  // "conflict": the server refuses to delete an account that still has Quizzes.
  const [deleteError, setDeleteError] = React.useState<
    "conflict" | "other" | undefined
  >();
  const navigate = useNavigate();
  const location = useLocation();
  const pathname = location.pathname;
  const dispatch = useAppDispatch();

  const handleOpenUserMenu = (event: React.MouseEvent<HTMLElement>) => {
    setAnchorEl(event.currentTarget);
  };

  const handleCloseUserMenu = () => {
    setAnchorEl(null);
  };

  const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === "Enter") {
      if (pathname.includes("/learning") || pathname.includes("/search")) {
        dispatch(searchPeriodContent(search)).then(() => navigate("/search"));
      } else if (pathname.includes("/quizzes")) {
        dispatch(searchQuizzes(search));
      }
    }
  };

  const userMenu = () => (
    <Menu
      id="menu-appbar"
      anchorEl={anchorEl}
      anchorOrigin={{
        vertical: "bottom", // Position the menu below the account icon
        horizontal: "right",
      }}
      keepMounted
      transformOrigin={{
        vertical: "top",
        horizontal: "right",
      }}
      open={Boolean(anchorEl)}
      onClose={handleCloseUserMenu}
    >
      {!authenticated ? (
        <MenuItem onClick={() => navigate("/register")}>
          Registriraj se
        </MenuItem>
      ) : (
        ""
      )}
      {!authenticated ? (
        <MenuItem onClick={() => navigate("/login")}>Prijavi se</MenuItem>
      ) : (
        ""
      )}
      {authenticated ? (
        <MenuItem onClick={handleLogout}>Odjavi se</MenuItem>
      ) : (
        ""
      )}
      {authenticated ? (
        <MenuItem
          onClick={() => {
            handleCloseUserMenu();
            setDeleteError(undefined);
            setDeleteDialogOpen(true);
          }}
        >
          Obriši račun
        </MenuItem>
      ) : (
        ""
      )}
    </Menu>
  );

  const handleLogout = (): void => {
    handleCloseUserMenu();
    dispatch(attemptLogout()).then(() => navigate("/learning"));
  };

  const handleDeleteAccount = (): void => {
    dispatch(deleteCurrentUser()).then((result) => {
      if (deleteCurrentUser.fulfilled.match(result)) {
        setDeleteDialogOpen(false);
        navigate("/learning");
      } else {
        setDeleteError(result.payload === 409 ? "conflict" : "other");
      }
    });
  };

  return (
    <AppBar position="fixed" open={toggle}>
      <Toolbar sx={{ display: "flex", justifyContent: "space-between" }}>
        <IconButton
          color="inherit"
          aria-label="open drawer"
          onClick={toggleSidebar}
          edge="start"
          sx={{
            marginRight: 5,
            ...(toggle && { display: "none" }),
          }}
        >
          <MenuIcon />
        </IconButton>
        <Search>
          <SearchIconWrapper>
            <SearchIcon />
          </SearchIconWrapper>
          <StyledInputBase
            placeholder="Pretraži..."
            inputProps={{ "aria-label": "search" }}
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            onKeyDown={handleKeyDown}
          />
        </Search>
        <Box>
          <IconButton
            size="large"
            edge="end"
            aria-label="account of current user"
            aria-controls={"primary-search-account-menu"}
            aria-haspopup="true"
            onClick={handleOpenUserMenu}
            color="inherit"
          >
            <AccountCircle />
          </IconButton>
          {anchorEl && userMenu()}
          <Dialog
            open={deleteDialogOpen}
            onClose={() => setDeleteDialogOpen(false)}
          >
            <DialogTitle>Obriši račun</DialogTitle>
            <DialogContent>
              <DialogContentText>
                Jeste li sigurni? Vaš račun i vaši rezultati bit će trajno
                obrisani.
              </DialogContentText>
              {deleteError && (
                <DialogContentText color="error" sx={{ marginTop: 2 }}>
                  {deleteError === "conflict"
                    ? "Račun se ne može obrisati dok imate kvizove. Prvo obrišite svoje kvizove u „Moji kvizovi”."
                    : "Račun nije obrisan. Pokušajte ponovno."}
                </DialogContentText>
              )}
            </DialogContent>
            <DialogActions>
              <Button onClick={() => setDeleteDialogOpen(false)}>
                Odustani
              </Button>
              <Button color="error" onClick={handleDeleteAccount}>
                Obriši
              </Button>
            </DialogActions>
          </Dialog>
        </Box>
      </Toolbar>
    </AppBar>
  );
}
