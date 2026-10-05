import { useParams } from "react-router-dom";
import { Fragment, useEffect, useMemo, useState } from "react";
import { RootState, useAppDispatch } from "../../store/store";
import { useSelector } from "react-redux";
import { getQuizAnalytics } from "../../store/slices/quizSlice";
import {
  Avatar,
  Box,
  Button,
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
import { pointsLabel } from "../../types/points";
import AnswerGroup from "../../components/AnswerGroup";
import { groupAnswers } from "../../components/AnswerGroup/group";

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

const share = (correctCount: number, answerCount: number) =>
  answerCount === 0
    ? "—"
    : `${correctCount} od ${answerCount} (${Math.round(
        (correctCount / answerCount) * 100
      )} %)`;

const mostChosenWrong = (row: {
  mostChosenWrongAnswer: string | null;
  mostChosenWrongCount: number;
}) =>
  row.mostChosenWrongAnswer === null
    ? "—"
    : `${row.mostChosenWrongAnswer} (${row.mostChosenWrongCount})`;

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
  // The matching or ordering Question whose lines are shown under its row.
  const [openQuestionId, setOpenQuestionId] = useState<number>();

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
                {analytics.questions.map((question, index) => (
                  <Fragment key={question.questionId}>
                  {/* A heading above the first of the Questions asked about one Source Text. */}
                  {question.sourceTextCaption !== null &&
                    question.sourceTextCaption !==
                      analytics.questions[index - 1]?.sourceTextCaption && (
                      <TableRow>
                        <TableCell colSpan={3} className="quiz-details-source">
                          Uz tekst: {question.sourceTextCaption}
                        </TableCell>
                      </TableRow>
                    )}
                  <TableRow>
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
                      {question.lines.length > 0 && (
                        <Button
                          size="small"
                          sx={{ display: "block", padding: 0 }}
                          aria-expanded={openQuestionId === question.questionId}
                          onClick={() =>
                            setOpenQuestionId(
                              openQuestionId === question.questionId
                                ? undefined
                                : question.questionId
                            )
                          }
                        >
                          {openQuestionId === question.questionId
                            ? "Sakrij pojmove"
                            : "Prikaži pojmove"}
                        </Button>
                      )}
                    </TableCell>
                    <TableCell align="right">
                      {share(question.correctCount, question.answerCount)}
                    </TableCell>
                    <TableCell className="quiz-details-text">
                      {mostChosenWrong(question)}
                    </TableCell>
                  </TableRow>
                  {/* A Question of several points, pair by pair or place by place. */}
                  {openQuestionId === question.questionId &&
                    question.lines.map((line) => (
                      <TableRow key={line.label} className="quiz-details-line">
                        <TableCell className="quiz-details-text">
                          {question.type === "ordering"
                            ? `${line.label}. mjesto`
                            : line.label}
                        </TableCell>
                        <TableCell align="right">
                          {share(line.correctCount, line.answerCount)}
                        </TableCell>
                        <TableCell className="quiz-details-text">
                          {mostChosenWrong(line)}
                        </TableCell>
                      </TableRow>
                    ))}
                  </Fragment>
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
                      {player.firstScore} / {player.firstMaxScore}{" "}
                      {pointsLabel(player.firstMaxScore)}
                    </TableCell>
                    <TableCell align="right">
                      {player.bestScore} / {player.bestMaxScore}{" "}
                      {pointsLabel(player.bestMaxScore)}
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
                          {quizGame.score} / {quizGame.answers.length}{" "}
                          {pointsLabel(quizGame.answers.length)}
                        </TableCell>
                      </TableRow>
                      {/* Rendered only while open, so the table's rows are its plays. */}
                      {open && (
                        <TableRow>
                          <TableCell colSpan={4} className="quiz-details-text">
                            {groupAnswers(quizGame.answers).map(
                              (answers, index) => (
                                <Box
                                  key={answers[0].id}
                                  className="quiz-details-answer"
                                >
                                  <AnswerGroup
                                    number={index + 1}
                                    answers={answers}
                                    answerLabel="Odgovor"
                                    dense
                                  />
                                </Box>
                              )
                            )}
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
