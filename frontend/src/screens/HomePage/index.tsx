import * as React from "react";
import Box from "@mui/material/Box";
import CssBaseline from "@mui/material/CssBaseline";
import IconButton from "@mui/material/IconButton";
import ChevronLeftIcon from "@mui/icons-material/ChevronLeft";
import ChevronRightIcon from "@mui/icons-material/ChevronRight";
import { Drawer, DrawerHeader, ScreenWrapper } from "./index.styled";
import Header from "./Header";
import Sidebar from "./Sidebar";
import { Outlet, useMatch } from "react-router-dom";
import { useSelector } from "react-redux";
import { RootState, useAppDispatch } from "../../store/store";
import { fetchTutorStatus, openTutor } from "../../store/slices/tutorSlice";
import logo from "/logo.svg";
import JoinQuiz from "../../components/JoinQuiz";
import Tutor from "../../components/Tutor";
import TutorButton from "../../components/Tutor/TutorButton";

export default function HomePage() {
  const dispatch = useAppDispatch();
  // On a phone the open drawer would leave the page a sliver, so there it starts collapsed.
  const [toggle, setToggle] = React.useState(() => window.innerWidth >= 900);
  const { authenticated, user } = useSelector((state: RootState) => state.user);
  const { periods } = useSelector((state: RootState) => state.period);
  const tutor = useSelector((state: RootState) => state.tutor);
  const [openJoinQuizDialog, setOpenJoinQuizDialog] = React.useState(false);

  // The tutor is offered where the material is read: the Period list, a Period and the
  // search. A Discussion, the quizzes and everything else have none.
  const onPeriodList = useMatch("/learning");
  const onPeriod = useMatch("/learning/:id");
  const onSearch = useMatch("/search");
  const tutorShown = tutor.available === true && Boolean(onPeriodList || onPeriod || onSearch);
  const tutorPeriod = periods?.find((period) => period.id === Number(onPeriod?.params.id));

  React.useEffect(() => {
    dispatch(fetchTutorStatus());
  }, [dispatch]);

  const toggleSidebar = () => {
    setToggle(!toggle);
  };

  return (
    <ScreenWrapper className={tutorShown && tutor.open ? "tutor-open" : undefined}>
      <CssBaseline />
      <Header toggle={toggle} />
      <Drawer variant="permanent" open={toggle}>
        <DrawerHeader
          sx={{
            minHeight: { xs: 88, sm: 88 },
            justifyContent: "center",
          }}
        >
          {toggle && (
            <Box
              component="img"
              alt="Prometej"
              src={logo}
              sx={{
                width: 75,
                height: 75,
                // In the middle of the room the chevron leaves it.
                margin: "0 auto",
                borderRadius: "50%",
                backgroundColor: "#e9e5cd",
              }}
            />
          )}
          <IconButton
            aria-label={toggle ? "Sakrij izbornik" : "Prikaži izbornik"}
            onClick={toggleSidebar}
          >
            {toggle ? <ChevronLeftIcon /> : <ChevronRightIcon />}
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
      {tutorShown && (
        <>
          {!tutor.open && (
            <TutorButton onClick={() => dispatch(openTutor())} />
          )}
          <Tutor periodId={tutorPeriod?.id ?? null} periodName={tutorPeriod?.name} />
        </>
      )}
      {openJoinQuizDialog && (
        <JoinQuiz setOpenJoinQuizDialog={setOpenJoinQuizDialog} />
      )}
    </ScreenWrapper>
  );
}
