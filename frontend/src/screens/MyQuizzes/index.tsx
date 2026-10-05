import {
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
  Grid,
  IconButton,
  Menu,
  MenuItem,
  Pagination,
  Paper,
  TextField,
  Typography,
} from "@mui/material";
import QuizContainer from "../../components/QuizContainer";
import { useEffect, useState } from "react";
import { RootState, useAppDispatch } from "../../store/store";
import {
  fetchMyQuizzes,
  deleteQuiz,
  updateQuiz,
} from "../../store/slices/quizSlice";
import { useSelector } from "react-redux";
import { QuizBaseModel } from "../../types/models/Quiz";
import "./styles.css";
import MoreVertIcon from "@mui/icons-material/MoreVert";
import { useNavigate } from "react-router-dom";

export default function MyQuizzes() {
  const [currentPage, setCurrentPage] = useState(1);
  // Two rows: a third row of cards does not fit on a 720 px high screen.
  const [postsPerPage] = useState(6);
  const { quizzes } = useSelector((state: RootState) => state.quiz);
  const { user } = useSelector((state: RootState) => state.user);
  const dispatch = useAppDispatch();
  // One menu for all the cards; it remembers which Quiz it was opened for. Closing clears
  // only the anchor, so the items do not change while the menu fades out.
  const [menu, setMenu] = useState<{
    anchor: HTMLElement | null;
    quiz: QuizBaseModel;
  } | null>(null);
  const [inputDrawer, setInputDrawer] = useState<boolean>(false);
  const [quizTitle, setQuizTitle] = useState<string>("");
  const [currentQuiz, setCurrentQuiz] = useState<QuizBaseModel | null>(null);
  // Kept after the dialog closes, so its text does not change while it fades out.
  const [quizToDelete, setQuizToDelete] = useState<QuizBaseModel | null>(null);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState<boolean>(false);
  const navigate = useNavigate();
  const handleClose = () => {
    setMenu((current) => current && { ...current, anchor: null });
  };

  const handleDelete = (quizId: number) => {
    setDeleteDialogOpen(false);
    dispatch(deleteQuiz(quizId)).then(() => {
      dispatch(fetchMyQuizzes());
    });
  };

  // The server gives a Quiz made private its Entry Code and takes it from one made public.
  const toggleVisibility = (quiz: QuizBaseModel) => {
    if (user) {
      const updatedQuiz = {
        id: quiz.id,
        title: quiz.title,
        isPrivate: !quiz.isPrivate,
        periodId: quiz.periodId,
      };
      dispatch(updateQuiz({ quiz: updatedQuiz })).then(() => {
        dispatch(fetchMyQuizzes());
      });
    }
    handleClose();
  };

  const handleNameChange = () => {
    if (currentQuiz && user) {
      const updatedQuiz = {
        id: currentQuiz.id,
        title: quizTitle.trim(),
        isPrivate: currentQuiz.isPrivate,
        periodId: currentQuiz.periodId,
      };
      dispatch(updateQuiz({ quiz: updatedQuiz })).then(() => {
        dispatch(fetchMyQuizzes());
      });
    }
    setInputDrawer(false);
  };

  useEffect(() => {
    if (user) dispatch(fetchMyQuizzes());
  }, [dispatch, user]);

  const indexOfLastPost = currentPage * postsPerPage;
  const indexOfFirstPost = indexOfLastPost - postsPerPage;
  const myQuizzes: QuizBaseModel[] = quizzes
    ? quizzes.slice(indexOfFirstPost, indexOfLastPost)
    : [];

  const handlePageChange = (value: number) => {
    setCurrentPage(value);
  };

  return (
    <Box
      sx={{
        flexGrow: 1,
        height: "90%",
        position: "relative",
        overflowY: "hidden",
      }}
    >
      <Grid
        container
        spacing={{ xs: 2, md: 3 }}
        columns={{ xs: 4, sm: 8, md: 12 }}
      >
        {myQuizzes.map((quiz: QuizBaseModel) => (
          <Grid
            item
            xs={2}
            sm={4}
            md={4}
            key={quiz.id}
            sx={{ position: "relative" }}
            onClick={(event) => {
              const target = event.target as HTMLElement;
              if (!target.closest(".quiz-container-opt")) {
                navigate(`/edit-quiz/${quiz.id}`);
              }
            }}
          >
            <QuizContainer
              name={quiz.title}
              authorName={quiz.creatorName}
              entryCode={quiz.entryCode}
              periodName={quiz.periodName}
              questionCount={quiz.questionCount}
            />
            <Box className="quiz-container-opt">
              <IconButton
                aria-label={`Mogućnosti kviza ${quiz.title}`}
                aria-haspopup="true"
                onClick={(event) => {
                  event.stopPropagation();
                  setMenu({ anchor: event.currentTarget, quiz });
                }}
              >
                <MoreVertIcon />
              </IconButton>
            </Box>
          </Grid>
        ))}
      </Grid>
      <Menu
        id="basic-menu"
        anchorEl={menu?.anchor}
        open={Boolean(menu?.anchor)}
        onClose={handleClose}
      >
        <MenuItem onClick={() => menu && toggleVisibility(menu.quiz)}>
          {menu?.quiz.isPrivate ? "Učini javnim" : "Učini privatnim"}
        </MenuItem>
        <MenuItem
          onClick={() => {
            if (menu) {
              setCurrentQuiz(menu.quiz);
              setQuizTitle(menu.quiz.title);
              setInputDrawer(true);
            }
            handleClose();
          }}
        >
          Promijeni ime
        </MenuItem>
        <MenuItem
          onClick={() => {
            if (menu) {
              setQuizToDelete(menu.quiz);
              setDeleteDialogOpen(true);
            }
            handleClose();
          }}
        >
          Izbriši
        </MenuItem>
        <MenuItem
          onClick={() => menu && navigate(`/quiz-details/${menu.quiz.id}`)}
        >
          Detalji
        </MenuItem>
      </Menu>
      <Dialog open={deleteDialogOpen} onClose={() => setDeleteDialogOpen(false)}>
        <DialogTitle>Obriši kviz</DialogTitle>
        <DialogContent>
          <DialogContentText>
            Jeste li sigurni da želite obrisati kviz „{quizToDelete?.title}”?
            {quizToDelete?.quizGameCount
              ? ` Broj spremljenih rezultata koji će biti trajno obrisani: ${quizToDelete.quizGameCount}.`
              : ""}
          </DialogContentText>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDeleteDialogOpen(false)}>Odustani</Button>
          <Button
            color="error"
            onClick={() => quizToDelete && handleDelete(quizToDelete.id)}
          >
            Obriši
          </Button>
        </DialogActions>
      </Dialog>
      <Pagination
        page={currentPage}
        count={quizzes ? Math.ceil(quizzes.length / postsPerPage) : 1}
        onChange={(_event, value: number) => handlePageChange(value)}
        className="pagination"
        sx={{
          position: "absolute",
          bottom: 0,
          left: "50%",
          transform: "translateX(-50%)",
        }}
      />
      {inputDrawer && (
        <Paper elevation={24} className="title-input">
          <Typography sx={{ marginTop: "1rem" }} variant="h5">
            Unesite naslov kviza
          </Typography>
          <TextField
            sx={{ marginTop: "3.5rem" }}
            type="text"
            placeholder="Naslov kviza"
            fullWidth
            required
            inputProps={{ maxLength: 100 }}
            value={quizTitle}
            onChange={(e) => setQuizTitle(e.target.value)}
          />
          <Button
            variant="contained"
            onClick={() => {
              quizTitle.trim() ? handleNameChange() : null;
            }}
            style={{
              backgroundColor: "#553b08",
              marginTop: "2rem",
              width: "80%",
            }}
          >
            Spremi
          </Button>
          <Button
            variant="contained"
            style={{
              backgroundColor: "#553b08",
              marginTop: "1rem",
              width: "80%",
            }}
            onClick={() => setInputDrawer(false)}
          >
            Odustani
          </Button>
        </Paper>
      )}
    </Box>
  );
}
