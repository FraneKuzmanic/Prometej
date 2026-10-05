import { useEffect } from "react";
import { useSelector } from "react-redux";
import { useNavigate } from "react-router-dom";
import {
  Box,
  Button,
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
import { RootState, useAppDispatch } from "../../store/store";
import { fetchMyGames } from "../../store/slices/quizSlice";
import { pointsLabel } from "../../types/points";
import "./styles.css";

const formatDate = (date: string) =>
  new Date(date).toLocaleDateString("hr-HR", {
    year: "numeric",
    month: "long",
    day: "numeric",
  });

export default function MyResults() {
  const { myGames, myGamesFailed } = useSelector(
    (state: RootState) => state.quiz
  );
  const dispatch = useAppDispatch();
  const navigate = useNavigate();

  useEffect(() => {
    const request = dispatch(fetchMyGames());
    return () => request.abort();
  }, [dispatch]);

  return (
    <Box className="my-results-wrapper">
      <Typography variant="h3" className="my-results-title">
        Moji rezultati
      </Typography>
      {myGamesFailed && (
        <Typography>Rezultati se nisu učitali. Pokušajte ponovno.</Typography>
      )}
      {myGames && myGames.games.length === 0 && (
        <Box>
          <Typography>Još niste riješili nijedan kviz.</Typography>
          <Button
            sx={{ marginTop: 2, backgroundColor: "#553b08" }}
            variant="contained"
            onClick={() => navigate("/quizzes")}
          >
            Kvizovi
          </Button>
        </Box>
      )}
      {myGames && myGames.progress.length > 0 && (
        <>
          <Typography variant="h5" className="my-results-heading">
            Napredak po razdobljima
          </Typography>
          <TableContainer component={Paper}>
            <Table aria-label="Napredak po razdobljima">
              <TableHead>
                <TableRow>
                  <TableCell className="my-results-column">Razdoblje</TableCell>
                  <TableCell className="my-results-column" align="right">
                    Riješeni kvizovi
                  </TableCell>
                  <TableCell className="my-results-column" align="right">
                    Prosjek najboljih rezultata
                  </TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {myGames.progress.map((period) => (
                  <TableRow
                    key={period.periodId}
                    hover
                    className="my-results-row"
                    onClick={() => navigate(`/quizzes?period=${period.periodId}`)}
                  >
                    <TableCell component="th" scope="row">
                      {/* The link makes the row reachable by keyboard; its click reaches the row. */}
                      <Link component="button" underline="hover" color="inherit">
                        {period.periodName}
                      </Link>
                    </TableCell>
                    <TableCell align="right">
                      {period.playedCount} od {period.quizCount}
                    </TableCell>
                    <TableCell align="right">
                      {period.averageBestPercent} %
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        </>
      )}
      {myGames && myGames.games.length > 0 && (
        <>
          <Typography variant="h5" className="my-results-heading">
            Riješeni kvizovi
          </Typography>
          <TableContainer component={Paper}>
            <Table aria-label="Riješeni kvizovi">
              <TableHead>
                <TableRow>
                  <TableCell className="my-results-column">Kviz</TableCell>
                  <TableCell className="my-results-column">Razdoblje</TableCell>
                  <TableCell className="my-results-column" align="right">
                    Datum rješavanja
                  </TableCell>
                  <TableCell className="my-results-column" align="right">
                    Rezultat
                  </TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {myGames.games.map((game) => (
                  <TableRow
                    key={game.id}
                    hover
                    className="my-results-row"
                    onClick={() => navigate(`/my-results/${game.id}`)}
                  >
                    <TableCell component="th" scope="row">
                      <Link component="button" underline="hover" color="inherit">
                        {game.quizTitle}
                      </Link>
                    </TableCell>
                    <TableCell>{game.periodName ?? "—"}</TableCell>
                    <TableCell align="right">
                      {formatDate(game.datePlayed)}
                    </TableCell>
                    <TableCell align="right">
                      {game.score} / {game.maxScore}{" "}
                      {pointsLabel(game.maxScore)}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        </>
      )}
    </Box>
  );
}
