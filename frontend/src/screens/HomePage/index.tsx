import * as React from "react";
import { useTheme } from "@mui/material/styles";
import Box from "@mui/material/Box";
import CssBaseline from "@mui/material/CssBaseline";
import IconButton from "@mui/material/IconButton";
import ChevronLeftIcon from "@mui/icons-material/ChevronLeft";
import ChevronRightIcon from "@mui/icons-material/ChevronRight";
import { Drawer, DrawerHeader, ScreenWrapper } from "./index.styled";
import { Avatar } from "@mui/material";
import Header from "./Header";
import Sidebar from "./Sidebar";
import { Outlet } from "react-router-dom";
import { useSelector } from "react-redux";
import { RootState } from "../../store/store";
import logo from "/logo.svg";
import JoinQuiz from "../../components/JoinQuiz";

export default function HomePage() {
  const theme = useTheme();
  const [toggle, setToggle] = React.useState(true);
  const { authenticated, user } = useSelector((state: RootState) => state.user);
  const [openJoinQuizDialog, setOpenJoinQuizDialog] = React.useState(false);

  const toggleSidebar = () => {
    setToggle(!toggle);
  };

  return (
    <ScreenWrapper>
      <CssBaseline />
      <Header toggle={toggle} toggleSidebar={toggleSidebar} />
      <Drawer variant="permanent" open={toggle}>
        <DrawerHeader
          sx={{
            display: "flex",
            justifyContent: "center",
          }}
        >
          <Box sx={{ paddingBottom: 1 }}>
            {toggle && (
              <Avatar
                alt="Prometej logo"
                src={logo}
                sx={{
                  width: 75,
                  height: 75,
                  backgroundColor: "#e9e5cd",
                  marginLeft: 8,
                }}
              />
            )}
          </Box>
          <IconButton onClick={toggleSidebar} sx={{ marginLeft: "auto" }}>
            {theme.direction === "rtl" ? (
              <ChevronRightIcon />
            ) : (
              <ChevronLeftIcon />
            )}
          </IconButton>
        </DrawerHeader>
        <Sidebar
          setOpenJoinQuizDialog={setOpenJoinQuizDialog}
          toggle={toggle}
          user={user}
          authenticated={authenticated}
        />
      </Drawer>
      <Box
        component="main"
        sx={{ flexGrow: 1, padding: 3, paddingBottom: 1, overflowY: "auto" }}
      >
        <DrawerHeader />
        <Outlet />
      </Box>
      {openJoinQuizDialog && (
        <JoinQuiz setOpenJoinQuizDialog={setOpenJoinQuizDialog} />
      )}
    </ScreenWrapper>
  );
}
