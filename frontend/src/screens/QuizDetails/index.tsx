import { useParams } from "react-router-dom";
import { Fragment, useEffect, useMemo, useState } from "react";
import { RootState, useAppDispatch } from "../../store/store";
import { useSelector } from "react-redux";
import { getQuizAnalytics } from "../../store/slices/quizSlice";
import {
  Avatar,
  Box,
  IconButton,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Typography,
} from "@mui/material";
import KeyboardArrowDownIcon from "@mui/icons-material/KeyboardArrowDown";
import KeyboardArrowUpIcon from "@mui/icons-material/KeyboardArrowUp";
import { QuizGameViewModel } from "../../types/models/Quiz";
import "./styles.css";
import { stringToColor } from "../../components/QuizContainer/stringToColor";
import { PieChart } from "@mui/x-charts/PieChart";

interface SeriesData {
  id: number;
  value: number;
  label: string;
}

const formatDate = (date: string) =>
  new Date(date).toLocaleDateString("hr-HR", {
    year: "numeric",
    month: "long",
    day: "numeric",
  });

function PlayerName({ name }: { name: string }) {
  return (
    <TableCell component="th" scope="row">
      {/* The flex box is inside the cell: a cell that is itself flex loses its row's border. */}
      <Box sx={{ display: "flex", alignItems: "center" }}>
        <Avatar sx={{ bgcolor: stringToColor(name) }}>
          {(name[0] ?? "?").toUpperCase()}
        </Avatar>
        <Typography
          sx={{ marginLeft: 1.5, fontSize: 14 }}
          variant="h6"
          component="div"
        >
          {name}
        </Typography>
      </Box>
    </TableCell>
  );
}

export function QuizDetails() {
  const { id } = useParams();
  const { analytics, analyticsFailed } = useSelector(
    (state: RootState) => state.quiz
  );
  const quizGames = analytics?.games;
  const dispatch = useAppDispatch();
  // The play whose answers are shown under its row; one at a time.
  const [openGameId, setOpenGameId] = useState<number>();

  // How many plays fall in each tenth of the percentage scale; empty tenths are left out.
  const seriesData = useMemo<SeriesData[]>(() => {
    const intervals = Array.from({ length: 10 }, (_, i) => i * 10);
    const intervalCounts = intervals.map(() => 0);
    (quizGames ?? []).forEach((game) => {
      // A play without answers has no percentage.
      if (game.answers.length === 0) return;
      const percentage = (game.score / game.answers.length) * 100;
      // The last interval includes 100%.
      intervalCounts[Math.min(9, Math.floor(percentage / 10))]++;
    });
    return intervals
      .map((interval, i) => ({
        id: i,
        value: intervalCounts[i],
        label: `${interval}% - ${interval + 10}%`,
      }))
      .filter((interval) => interval.value > 0);
  }, [quizGames]);

  useEffect(() => {
    if (!id) return;
    const request = dispatch(getQuizAnalytics(parseInt(id)));
    // Leaving for another quiz drops this request, so its late answer cannot replace that quiz's plays.
    return () => request.abort();
  }, [dispatch, id]);

  return (
    <Box className="quiz-game-details-wrapper">
      <Typography variant="h3" className="quiz-game-details-title">
        Analitika kviza
      </Typography>
      {analytics && (
        <Typography variant="h5" className="quiz-game-details-message">
          {analytics.quizTitle}
        </Typography>
      )}
      {analyticsFailed && (
        <Typography className="quiz-game-details-message">
          Analitika nije dostupna.
        </Typography>
      )}
      {quizGames && quizGames.length === 0 && (
        <Typography className="quiz-game-details-message">
          Još nitko nije riješio ovaj kviz.
        </Typography>
      )}
      {quizGames && quizGames.length > 0 && (
        <Box className="pie-chart-container">
          <PieChart
            series={[
              {
                data: seriesData,
              },
            ]}
            width={400}
            height={200}
          />
        </Box>
      )}
      {analytics && analytics.games.length > 0 && (
        <>
          <Typography variant="h5" className="quiz-details-heading">
            Uspjeh po pitanjima
          </Typography>
          <TableContainer component={Paper}>
            <Table aria-label="Uspjeh po pitanjima">
              <TableHead>
                <TableRow>
                  <TableCell className="quiz-details-column">Pitanje</TableCell>
                  <TableCell className="quiz-details-column" align="right">
                    Točni odgovori
                  </TableCell>
                  <TableCell className="quiz-details-column">
                    Najčešći pogrešan odgovor
                  </TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {analytics.questions.map((question) => (
                  <TableRow key={question.questionId}>
                    <TableCell
                      component="th"
                      scope="row"
                      className="quiz-details-text"
                    >
                      {question.questionTitle}
                      {question.isRetired && (
                        <Typography
                          variant="caption"
                          color="text.secondary"
                          display="block"
                        >
                          Uklonjeno iz kviza
                        </Typography>
                      )}
                    </TableCell>
                    <TableCell align="right">
                      {question.answerCount === 0
                        ? "—"
                        : `${question.correctCount} od ${
                            question.answerCount
                          } (${Math.round(
                            (question.correctCount / question.answerCount) * 100
                          )} %)`}
                    </TableCell>
                    <TableCell className="quiz-details-text">
                      {question.mostChosenWrongAnswer === null
                        ? "—"
                        : `${question.mostChosenWrongAnswer} (${question.mostChosenWrongCount})`}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
          <Typography variant="h5" className="quiz-details-heading">
            Učenici
          </Typography>
          <TableContainer component={Paper}>
            <Table aria-label="Učenici">
              <TableHead>
                <TableRow>
                  <TableCell className="quiz-details-column">
                    Ime i prezime
                  </TableCell>
                  <TableCell className="quiz-details-column" align="right">
                    Broj rješavanja
                  </TableCell>
                  <TableCell className="quiz-details-column" align="right">
                    Prvi rezultat
                  </TableCell>
                  <TableCell className="quiz-details-column" align="right">
                    Najbolji rezultat
                  </TableCell>
                  <TableCell className="quiz-details-column" align="right">
                    Zadnje rješavanje
                  </TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {analytics.players.map((player) => (
                  <TableRow key={player.userId}>
                    <PlayerName name={player.userName} />
                    <TableCell align="right">{player.gameCount}</TableCell>
                    <TableCell align="right">
                      {player.firstScore} / {player.firstQuestionCount}
                    </TableCell>
                    <TableCell align="right">
                      {player.bestScore} / {player.bestQuestionCount}
                    </TableCell>
                    <TableCell align="right">
                      {formatDate(player.lastPlayed)}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
          <Typography variant="h5" className="quiz-details-heading">
            Sva rješavanja
          </Typography>
          <TableContainer component={Paper}>
            <Table aria-label="Sva rješavanja">
              <TableHead>
                <TableRow>
                  <TableCell />
                  <TableCell className="quiz-details-column">
                    Ime i prezime
                  </TableCell>
                  <TableCell className="quiz-details-column" align="right">
                    Datum rješavanja
                  </TableCell>
                  <TableCell className="quiz-details-column" align="right">
                    Ostvareni rezultat
                  </TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {analytics.games.map((quizGame: QuizGameViewModel) => {
                  const open = openGameId === quizGame.id;
                  return (
                    <Fragment key={quizGame.id}>
                      <TableRow>
                        <TableCell padding="checkbox">
                          <IconButton
                            size="small"
                            aria-label={
                              open ? "Sakrij odgovore" : "Prikaži odgovore"
                            }
                            aria-expanded={open}
                            onClick={() =>
                              setOpenGameId(open ? undefined : quizGame.id)
                            }
                          >
                            {open ? (
                              <KeyboardArrowUpIcon />
                            ) : (
                              <KeyboardArrowDownIcon />
                            )}
                          </IconButton>
                        </TableCell>
                        <PlayerName name={quizGame.userName} />
                        <TableCell align="right">
                          {formatDate(quizGame.datePlayed)}
                        </TableCell>
                        <TableCell align="right">
                          {quizGame.score} / {quizGame.answers.length}
                        </TableCell>
                      </TableRow>
                      {/* Rendered only while open, so the table's rows are its plays. */}
                      {open && (
                        <TableRow>
                          <TableCell colSpan={4} className="quiz-details-text">
                            {quizGame.answers.map((answer, index) => {
                              const correct =
                                answer.answerText === answer.correctAnswer;
                              return (
                                <Box key={answer.id} className="quiz-details-answer">
                                  <Typography variant="subtitle2">
                                    {index + 1}. {answer.questionTitle}
                                  </Typography>
                                  <Typography
                                    variant="body2"
                                    className={
                                      correct
                                        ? "quiz-details-correct"
                                        : "quiz-details-wrong"
                                    }
                                  >
                                    Odgovor: {answer.answerText}
                                  </Typography>
                                  {!correct && (
                                    <Typography variant="body2">
                                      Točan odgovor: {answer.correctAnswer}
                                    </Typography>
                                  )}
                                </Box>
                              );
                            })}
                          </TableCell>
                        </TableRow>
                      )}
                    </Fragment>
                  );
                })}
              </TableBody>
            </Table>
          </TableContainer>
        </>
      )}
    </Box>
  );
}
