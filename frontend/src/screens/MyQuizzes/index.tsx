import {
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
  Divider,
  ListItemIcon,
  Menu,
  MenuItem,
  Pagination,
  TextField,
} from "@mui/material";
import AddIcon from "@mui/icons-material/Add";
import BarChartOutlinedIcon from "@mui/icons-material/BarChartOutlined";
import ContentCopyOutlinedIcon from "@mui/icons-material/ContentCopyOutlined";
import DeleteOutlineIcon from "@mui/icons-material/DeleteOutline";
import DriveFileRenameOutlineIcon from "@mui/icons-material/DriveFileRenameOutline";
import EditOutlinedIcon from "@mui/icons-material/EditOutlined";
import LockOutlinedIcon from "@mui/icons-material/LockOutlined";
import PublicOutlinedIcon from "@mui/icons-material/PublicOutlined";
import QuizOutlinedIcon from "@mui/icons-material/QuizOutlined";
import QuizContainer from "../../components/QuizContainer";
import { EmptyState, Page, PageHeader } from "../../components/Page";
import { FormEvent, useEffect, useState } from "react";
import { RootState, useAppDispatch } from "../../store/store";
import {
  fetchMyQuizzes,
  copyQuiz,
  deleteQuiz,
  updateQuiz,
} from "../../store/slices/quizSlice";
import { fetchPeriods } from "../../store/slices/periodSlice";
import { useSelector } from "react-redux";
import { QuizBaseModel, QuizEditRequest } from "../../types/models/Quiz";
import { useNavigate } from "react-router-dom";

const postsPerPage = 9;

export default function MyQuizzes() {
  const [currentPage, setCurrentPage] = useState(1);
  const { quizzes } = useSelector((state: RootState) => state.quiz);
  const { periods } = useSelector((state: RootState) => state.period);
  const { user } = useSelector((state: RootState) => state.user);
  const dispatch = useAppDispatch();
  // One menu for all the cards; it remembers which Quiz it was opened for. Closing clears
  // only the anchor, so the items do not change while the menu fades out.
  const [menu, setMenu] = useState<{
    anchor: HTMLElement | null;
    quiz: QuizBaseModel;
  } | null>(null);
  const [renameOpen, setRenameOpen] = useState<boolean>(false);
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

  const handleCopy = (quizId: number) => {
    dispatch(copyQuiz(quizId)).then(() => {
      dispatch(fetchMyQuizzes());
    });
    handleClose();
  };

  // An update carries the Quiz's whole header, so a change to one field sends the others
  // as the card has them.
  const headerOf = (quiz: QuizBaseModel): QuizEditRequest => ({
    id: quiz.id,
    title: quiz.title,
    isPrivate: quiz.isPrivate,
    periodId: quiz.periodId,
    isTest: quiz.isTest,
    timeLimitMinutes: quiz.timeLimitMinutes,
    closesAt: quiz.closesAt,
  });

  // The server gives a Quiz made private its Entry Code and takes it from one made public.
  const toggleVisibility = (quiz: QuizBaseModel) => {
    if (user) {
      const updatedQuiz = { ...headerOf(quiz), isPrivate: !quiz.isPrivate };
      dispatch(updateQuiz({ quiz: updatedQuiz })).then(() => {
        dispatch(fetchMyQuizzes());
      });
    }
    handleClose();
  };

  const handleNameChange = (event: FormEvent) => {
    event.preventDefault();
    if (!quizTitle.trim()) return;
    if (currentQuiz && user) {
      const updatedQuiz = { ...headerOf(currentQuiz), title: quizTitle.trim() };
      dispatch(updateQuiz({ quiz: updatedQuiz })).then(() => {
        dispatch(fetchMyQuizzes());
      });
    }
    setRenameOpen(false);
  };

  useEffect(() => {
    if (user) dispatch(fetchMyQuizzes());
  }, [dispatch, user]);

  // The cards show the painting of a Quiz's Period.
  useEffect(() => {
    if (!periods) dispatch(fetchPeriods());
  }, [dispatch, periods]);

  const indexOfLastPost = currentPage * postsPerPage;
  const indexOfFirstPost = indexOfLastPost - postsPerPage;
  const myQuizzes: QuizBaseModel[] = quizzes
    ? quizzes.slice(indexOfFirstPost, indexOfLastPost)
    : [];

  const handlePageChange = (value: number) => {
    setCurrentPage(value);
  };

  return (
    <Page>
      <PageHeader
        title="Moji kvizovi"
        lead="Kvizovi koje ste sastavili. Kartica otvara uređivanje, a izbornik na njoj rezultate i ostale mogućnosti."
      >
        <Button
          variant="contained"
          startIcon={<AddIcon />}
          onClick={() => navigate("/make-quiz")}
        >
          Novi kviz
        </Button>
      </PageHeader>
      {quizzes?.length === 0 && (
        <EmptyState
          icon={<QuizOutlinedIcon />}
          title="Još nemate nijedan kviz."
          action={
            <Button variant="contained" onClick={() => navigate("/make-quiz")}>
              Sastavi prvi kviz
            </Button>
          }
        >
          Sastavite kviz za vježbu ili provjeru: javni vide svi, a privatni otvara samo
          razred kojemu date ulazni kod.
        </EmptyState>
      )}
      <div className="quiz-grid">
        {myQuizzes.map((quiz: QuizBaseModel) => (
          <QuizContainer
            key={quiz.id}
            quiz={quiz}
            own
            image={periods?.find((period) => period.id === quiz.periodId)?.image}
            onOpen={() => navigate(`/edit-quiz/${quiz.id}`)}
            onMenu={(anchor) => setMenu({ anchor, quiz })}
          />
        ))}
      </div>
      <Menu
        id="basic-menu"
        anchorEl={menu?.anchor}
        open={Boolean(menu?.anchor)}
        onClose={handleClose}
      >
        <MenuItem onClick={() => menu && navigate(`/edit-quiz/${menu.quiz.id}`)}>
          <ListItemIcon>
            <EditOutlinedIcon fontSize="small" />
          </ListItemIcon>
          Uredi
        </MenuItem>
        <MenuItem
          onClick={() => menu && navigate(`/quiz-details/${menu.quiz.id}`)}
        >
          <ListItemIcon>
            <BarChartOutlinedIcon fontSize="small" />
          </ListItemIcon>
          Analitika kviza
        </MenuItem>
        <Divider />
        {/* A Test is a Private Quiz; it is made practice again in the editor first. */}
        {!menu?.quiz.isTest && (
          <MenuItem onClick={() => menu && toggleVisibility(menu.quiz)}>
            <ListItemIcon>
              {menu?.quiz.isPrivate ? (
                <PublicOutlinedIcon fontSize="small" />
              ) : (
                <LockOutlinedIcon fontSize="small" />
              )}
            </ListItemIcon>
            {menu?.quiz.isPrivate ? "Učini javnim" : "Učini privatnim"}
          </MenuItem>
        )}
        <MenuItem
          onClick={() => {
            if (menu) {
              setCurrentQuiz(menu.quiz);
              setQuizTitle(menu.quiz.title);
              setRenameOpen(true);
            }
            handleClose();
          }}
        >
          <ListItemIcon>
            <DriveFileRenameOutlineIcon fontSize="small" />
          </ListItemIcon>
          Promijeni ime
        </MenuItem>
        <MenuItem onClick={() => menu && handleCopy(menu.quiz.id)}>
          <ListItemIcon>
            <ContentCopyOutlinedIcon fontSize="small" />
          </ListItemIcon>
          Kopiraj kviz
        </MenuItem>
        <Divider />
        <MenuItem
          className="menu-item-danger"
          onClick={() => {
            if (menu) {
              setQuizToDelete(menu.quiz);
              setDeleteDialogOpen(true);
            }
            handleClose();
          }}
        >
          <ListItemIcon>
            <DeleteOutlineIcon fontSize="small" />
          </ListItemIcon>
          Obriši
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
      {quizzes && quizzes.length > postsPerPage && (
        <Pagination
          page={currentPage}
          color="primary"
          count={Math.ceil(quizzes.length / postsPerPage)}
          onChange={(_event, value: number) => handlePageChange(value)}
          className="pagination quiz-pagination"
        />
      )}
      <Dialog
        open={renameOpen}
        onClose={() => setRenameOpen(false)}
        maxWidth="xs"
        fullWidth
        PaperProps={{
          component: "form",
          onSubmit: handleNameChange,
          className: "title-input",
        }}
      >
        <DialogTitle>Promijeni ime kviza</DialogTitle>
        <DialogContent>
          <TextField
            sx={{ marginTop: 1 }}
            label="Naslov kviza"
            placeholder="Naslov kviza"
            InputLabelProps={{ shrink: true }}
            autoFocus
            fullWidth
            inputProps={{ maxLength: 100 }}
            value={quizTitle}
            onChange={(e) => setQuizTitle(e.target.value)}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setRenameOpen(false)}>Odustani</Button>
          <Button type="submit" variant="contained" disabled={!quizTitle.trim()}>
            Spremi
          </Button>
        </DialogActions>
      </Dialog>
    </Page>
  );
}
