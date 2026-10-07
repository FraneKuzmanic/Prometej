import { useEffect } from "react";
import { useSelector } from "react-redux";
import { useNavigate } from "react-router-dom";
import {
  Box,
  Button,
  Chip,
  Link,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Typography,
} from "@mui/material";
import ChevronRightIcon from "@mui/icons-material/ChevronRight";
import EmojiEventsOutlinedIcon from "@mui/icons-material/EmojiEventsOutlined";
import { RootState, useAppDispatch } from "../../store/store";
import { fetchMyGames } from "../../store/slices/quizSlice";
import { fetchPeriods } from "../../store/slices/periodSlice";
import { pointsLabel } from "../../types/points";
import { EmptyState, Page, PageHeader } from "../../components/Page";
import "./styles.css";

const formatDate = (date: string) =>
  new Date(date).toLocaleDateString("hr-HR", {
    year: "numeric",
    month: "long",
    day: "numeric",
  });

// A share as a bar: the number beside it says the same thing.
function Meter({ percent }: { percent: number }) {
  return (
    <span className="my-results-meter" aria-hidden="true">
      <span style={{ width: `${Math.max(0, Math.min(100, percent))}%` }} />
    </span>
  );
}

export default function MyResults() {
  const { myGames, myGamesFailed } = useSelector(
    (state: RootState) => state.quiz
  );
  const { periods } = useSelector((state: RootState) => state.period);
  const dispatch = useAppDispatch();
  const navigate = useNavigate();

  useEffect(() => {
    const request = dispatch(fetchMyGames());
    return () => request.abort();
  }, [dispatch]);

  // A row of progress shows its Period's painting.
  useEffect(() => {
    if (!periods) dispatch(fetchPeriods());
  }, [dispatch, periods]);

  return (
    <Page>
      <PageHeader
        title="Moji rezultati"
        lead="Svaki kviz koji ste riješili prijavljeni, s odgovorima, i vaš napredak po razdobljima."
      />
      {myGamesFailed && (
        <Typography>Rezultati se nisu učitali. Pokušajte ponovno.</Typography>
      )}
      {myGames && myGames.games.length === 0 && (
        <EmptyState
          icon={<EmojiEventsOutlinedIcon />}
          title="Još niste riješili nijedan kviz."
          action={
            <>
              <Button variant="contained" onClick={() => navigate("/quizzes")}>
                Odaberi kviz
              </Button>
              <Button variant="outlined" onClick={() => navigate("/learning")}>
                Prvo pročitaj gradivo
              </Button>
            </>
          }
        >
          Riješite kviz do kraja i ovdje će ostati vaš rezultat, pregled svakog odgovora
          i napredak po razdobljima.
        </EmptyState>
      )}
      {myGames && myGames.progress.length > 0 && (
        <section className="page-section my-results-first">
          <h2>Napredak po razdobljima</h2>
          <p className="page-section-note">
            Koliko ste javnih kvizova razdoblja riješili i prosjek najboljeg rezultata
            na svakom od njih.
          </p>
          <TableContainer component={Paper}>
            <Table aria-label="Napredak po razdobljima">
              <TableHead>
                <TableRow>
                  <TableCell>Razdoblje</TableCell>
                  <TableCell align="right">Riješeni kvizovi</TableCell>
                  <TableCell align="right">Prosjek najboljih rezultata</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {myGames.progress.map((period) => {
                  const image = periods?.find((p) => p.id === period.periodId)?.image;
                  return (
                    <TableRow
                      key={period.periodId}
                      hover
                      className="my-results-row"
                      onClick={() => navigate(`/quizzes?period=${period.periodId}`)}
                    >
                      <TableCell component="th" scope="row">
                        {/* The flex box is inside the cell: a cell that is itself flex
                            loses its row's bottom border. */}
                        <Box className="my-results-period">
                          {image && <img src={`/${image}`} alt="" />}
                          {/* The link makes the row reachable by keyboard; its click reaches the row. */}
                          <Link component="button" underline="hover" color="inherit">
                            {period.periodName}
                          </Link>
                        </Box>
                      </TableCell>
                      <TableCell align="right">
                        {period.playedCount} od {period.quizCount}
                      </TableCell>
                      <TableCell align="right">
                        <Box className="my-results-score">
                          <Meter percent={period.averageBestPercent} />
                          <span>{period.averageBestPercent} %</span>
                        </Box>
                      </TableCell>
                    </TableRow>
                  );
                })}
              </TableBody>
            </Table>
          </TableContainer>
        </section>
      )}
      {myGames && myGames.games.length > 0 && (
        <section className="page-section">
          <h2>Riješeni kvizovi</h2>
          <TableContainer component={Paper}>
            <Table aria-label="Riješeni kvizovi">
              <TableHead>
                <TableRow>
                  <TableCell>Kviz</TableCell>
                  <TableCell>Razdoblje</TableCell>
                  <TableCell align="right">Datum rješavanja</TableCell>
                  <TableCell align="right">Rezultat</TableCell>
                  <TableCell padding="checkbox" />
                </TableRow>
              </TableHead>
              <TableBody>
                {myGames.games.map((game) => (
                  <TableRow
                    key={game.id}
                    hover={game.reviewAvailable}
                    className={game.reviewAvailable ? "my-results-row" : undefined}
                    onClick={() =>
                      game.reviewAvailable && navigate(`/my-results/${game.id}`)
                    }
                  >
                    <TableCell component="th" scope="row">
                      {/* The flex box is inside the cell: a cell that is itself flex
                          loses its row's bottom border. */}
                      <Box className="my-results-quiz">
                        {game.reviewAvailable ? (
                          <Link component="button" underline="hover" color="inherit">
                            {game.quizTitle}
                          </Link>
                        ) : (
                          game.quizTitle
                        )}
                        {game.playedAs !== "practice" && (
                          <Chip
                            size="small"
                            label={game.playedAs === "test" ? "Provjera" : "Kao provjera"}
                          />
                        )}
                      </Box>
                      {!game.reviewAvailable && (
                        <Typography variant="body2" color="text.secondary">
                          odgovori nakon završetka
                        </Typography>
                      )}
                    </TableCell>
                    <TableCell>{game.periodName ?? "—"}</TableCell>
                    <TableCell align="right">
                      {formatDate(game.datePlayed)}
                    </TableCell>
                    <TableCell align="right">
                      <Box className="my-results-score">
                        <Meter
                          percent={game.maxScore ? (game.score / game.maxScore) * 100 : 0}
                        />
                        <span>
                          {game.score} / {game.maxScore} {pointsLabel(game.maxScore)}
                        </span>
                      </Box>
                    </TableCell>
                    <TableCell padding="checkbox" className="my-results-open">
                      {game.reviewAvailable && <ChevronRightIcon fontSize="small" />}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        </section>
      )}
    </Page>
  );
}
