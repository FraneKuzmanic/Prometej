import {
  Button,
  ListItemIcon,
  ListItemText,
  Menu,
  MenuItem,
  Pagination,
  TextField,
} from "@mui/material";
import FactCheckOutlinedIcon from "@mui/icons-material/FactCheckOutlined";
import KeyOutlinedIcon from "@mui/icons-material/KeyOutlined";
import PlayArrowOutlinedIcon from "@mui/icons-material/PlayArrowOutlined";
import SearchOffIcon from "@mui/icons-material/SearchOff";
import QuizContainer from "../../components/QuizContainer";
import JoinQuiz from "../../components/JoinQuiz";
import { EmptyState, Page, PageHeader } from "../../components/Page";
import { useEffect, useState } from "react";
import { RootState, useAppDispatch } from "../../store/store";
import { searchQuizzes } from "../../store/slices/quizSlice";
import { fetchPeriods } from "../../store/slices/periodSlice";
import { useSelector } from "react-redux";
import { QuizBaseModel } from "../../types/models/Quiz";
import { useLocation, useNavigate, useSearchParams } from "react-router-dom";

const postsPerPage = 9;

export default function Quizzes() {
  const [currentPage, setCurrentPage] = useState(1);
  const { quizzes } = useSelector((state: RootState) => state.quiz);
  const { periods } = useSelector((state: RootState) => state.period);
  const dispatch = useAppDispatch();
  const navigate = useNavigate();
  // One menu for all the cards; it remembers which Quiz it was opened for. Closing clears
  // only the anchor, so the items do not change while the menu fades out.
  const [menu, setMenu] = useState<{
    anchor: HTMLElement | null;
    quiz: QuizBaseModel;
  } | null>(null);
  const [joinOpen, setJoinOpen] = useState(false);
  // The query and the Period are in the address, so a Period's page can link to its
  // Quizzes and a filtered list can be reloaded.
  const [searchParams, setSearchParams] = useSearchParams();
  const query = (searchParams.get("q") ?? "").trim();
  const periodParam = Number(searchParams.get("period"));
  // Anything that cannot be a Period's id is no filter; the server would refuse a number
  // that does not fit its integer.
  const periodId =
    Number.isInteger(periodParam) && periodParam > 0 && periodParam < 2 ** 31
      ? periodParam
      : undefined;
  // Changes on every navigation, so Enter on the same query searches again.
  const { key } = useLocation();

  useEffect(() => {
    const request = dispatch(searchQuizzes({ query, periodId }));
    // A newer search drops this request, so its late answer cannot replace the newer list.
    return () => request.abort();
  }, [dispatch, query, periodId, key]);

  useEffect(() => {
    if (!periods) dispatch(fetchPeriods());
  }, [dispatch, periods]);

  // A search can leave fewer pages than the one that was open.
  useEffect(() => {
    setCurrentPage(1);
  }, [quizzes]);

  const indexOfLastPost = currentPage * postsPerPage;
  const indexOfFirstPost = indexOfLastPost - postsPerPage;
  const currentPosts: QuizBaseModel[] = quizzes
    ? quizzes.slice(indexOfFirstPost, indexOfLastPost)
    : [];

  const handlePageChange = (value: number) => {
    setCurrentPage(value);
  };

  const handlePeriodChange = (value: string) => {
    const params = new URLSearchParams(searchParams);
    if (value) params.set("period", value);
    else params.delete("period");
    setSearchParams(params);
  };

  const closeMenu = () => setMenu((current) => current && { ...current, anchor: null });
  const filtered = query !== "" || periodId !== undefined;

  return (
    <Page>
      <PageHeader
        title="Kvizovi"
        lead={
          query
            ? `Javni kvizovi čiji naslov ili autor sadrži „${query}”.`
            : "Javni kvizovi za vježbu. Točan odgovor vidite nakon svakog pitanja."
        }
      >
        <TextField
          select
          size="small"
          label="Razdoblje"
          sx={{ width: 240 }}
          // Empty until the list is here, and for a Period the list does not have.
          value={periods?.some((p) => p.id === periodId) ? String(periodId) : ""}
          onChange={(e) => handlePeriodChange(e.target.value)}
          SelectProps={{ displayEmpty: true }}
          InputLabelProps={{ shrink: true }}
        >
          <MenuItem value="">Sva razdoblja</MenuItem>
          {periods?.map((p) => (
            <MenuItem key={p.id} value={String(p.id)}>
              {p.name}
            </MenuItem>
          ))}
        </TextField>
        <Button
          variant="outlined"
          startIcon={<KeyOutlinedIcon />}
          onClick={() => setJoinOpen(true)}
        >
          Imam ulazni kod
        </Button>
      </PageHeader>
      <div className="quiz-grid">
        {currentPosts.map((quiz) => (
          <QuizContainer
            key={quiz.id}
            quiz={quiz}
            image={periods?.find((period) => period.id === quiz.periodId)?.image}
            onOpen={() => navigate(`/play-quiz/${quiz.id}`)}
            onMenu={(anchor) => setMenu({ anchor, quiz })}
          />
        ))}
      </div>
      {/* The card itself starts practice; the menu also offers the same Quiz without
          feedback until the end. */}
      <Menu anchorEl={menu?.anchor} open={Boolean(menu?.anchor)} onClose={closeMenu}>
        <MenuItem onClick={() => menu && navigate(`/play-quiz/${menu.quiz.id}`)}>
          <ListItemIcon>
            <PlayArrowOutlinedIcon fontSize="small" />
          </ListItemIcon>
          <ListItemText
            primary="Vježbaj"
            secondary="Točan odgovor nakon svakog pitanja"
          />
        </MenuItem>
        <MenuItem onClick={() => menu && navigate(`/sitting/${menu.quiz.id}`)}>
          <ListItemIcon>
            <FactCheckOutlinedIcon fontSize="small" />
          </ListItemIcon>
          <ListItemText
            primary="Riješi kao provjeru"
            secondary="Rezultat tek nakon predaje"
          />
        </MenuItem>
      </Menu>
      {quizzes?.length === 0 ? (
        <EmptyState
          icon={<SearchOffIcon />}
          title="Nema kvizova za prikaz."
          action={
            filtered && (
              <Button variant="outlined" onClick={() => navigate("/quizzes")}>
                Prikaži sve kvizove
              </Button>
            )
          }
        >
          {filtered
            ? "Nijedan javni kviz ne odgovara pretrazi ili odabranom razdoblju."
            : "Još nitko nije objavio javni kviz."}
        </EmptyState>
      ) : (
        quizzes &&
        quizzes.length > postsPerPage && (
          <Pagination
            page={currentPage}
            color="primary"
            count={Math.ceil(quizzes.length / postsPerPage)}
            onChange={(_event, value: number) => handlePageChange(value)}
            className="pagination quiz-pagination"
          />
        )
      )}
      {joinOpen && <JoinQuiz setOpenJoinQuizDialog={setJoinOpen} />}
    </Page>
  );
}
