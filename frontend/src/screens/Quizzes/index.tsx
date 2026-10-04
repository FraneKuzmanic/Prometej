import {
  Box,
  Grid,
  MenuItem,
  Pagination,
  TextField,
  Typography,
} from "@mui/material";
import QuizContainer from "../../components/QuizContainer";
import { useEffect, useState } from "react";
import { RootState, useAppDispatch } from "../../store/store";
import { searchQuizzes } from "../../store/slices/quizSlice";
import { fetchPeriods } from "../../store/slices/periodSlice";
import { useSelector } from "react-redux";
import { QuizBaseModel } from "../../types/models/Quiz";
import { useLocation, useNavigate, useSearchParams } from "react-router-dom";
import "./styles.css";

export default function Quizzes() {
  const [currentPage, setCurrentPage] = useState(1);
  // Two rows: a third does not fit under the filter on a 720 px high screen.
  const [postsPerPage] = useState(6);
  const { quizzes } = useSelector((state: RootState) => state.quiz);
  const { periods } = useSelector((state: RootState) => state.period);
  const dispatch = useAppDispatch();
  const navigate = useNavigate();
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

  return (
    <Box
      sx={{
        flexGrow: 1,
        height: "90%",
        position: "relative",
        overflowY: "hidden",
      }}
    >
      <TextField
        select
        size="small"
        label="Razdoblje"
        sx={{ display: "flex", width: 260, marginTop: 1, marginBottom: 2 }}
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
      <Grid
        container
        spacing={{ xs: 2, md: 3 }}
        columns={{ xs: 4, sm: 8, md: 12 }}
      >
        {currentPosts.map((quiz) => (
          <Grid
            item
            xs={2}
            sm={4}
            md={4}
            key={quiz.id}
            onClick={() => navigate(`/play-quiz/${quiz.id}`)}
          >
            <QuizContainer
              name={quiz.title}
              authorName={quiz.creatorName}
              periodName={quiz.periodName}
              questionCount={quiz.questionCount}
            />
          </Grid>
        ))}
      </Grid>
      {quizzes?.length === 0 ? (
        <Typography>Nema kvizova za prikaz.</Typography>
      ) : (
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
      )}
    </Box>
  );
}
