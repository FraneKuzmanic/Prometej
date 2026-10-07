import { Link as RouterLink, useParams } from "react-router-dom";
import { Fragment, useEffect, useMemo, useState } from "react";
import { RootState, useAppDispatch } from "../../store/store";
import { useSelector } from "react-redux";
import { closeTest, getQuizAnalytics } from "../../store/slices/quizSlice";
import { resetSitting } from "../../store/slices/sittingSlice";
import {
  Box,
  Button,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
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
import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import BarChartOutlinedIcon from "@mui/icons-material/BarChartOutlined";
import KeyboardArrowDownIcon from "@mui/icons-material/KeyboardArrowDown";
import KeyboardArrowUpIcon from "@mui/icons-material/KeyboardArrowUp";
import { QuizGameViewModel, SittingRow } from "../../types/models/Quiz";
import { formatDateTime } from "../Discussion/format";
import "./styles.css";
import Initials from "../../components/Initials";
import { BarChart } from "@mui/x-charts/BarChart";
import { EmptyState, Page, PageHeader } from "../../components/Page";
import { pointsLabel } from "../../types/points";
import AnswerGroup from "../../components/AnswerGroup";
import { groupAnswers } from "../../components/AnswerGroup/group";

// The ten tenths of the percentage scale a play can fall in.
const tenths = Array.from({ length: 10 }, (_, i) => `${i * 10}–${i * 10 + 10} %`);

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

// A share with its bar before it, so a column of them shows the weak Questions at a glance.
function Share({ correctCount, answerCount }: { correctCount: number; answerCount: number }) {
  return (
    <Box className="quiz-details-share">
      {answerCount > 0 && (
        <span className="quiz-details-meter" aria-hidden="true">
          <span style={{ width: `${(correctCount / answerCount) * 100}%` }} />
        </span>
      )}
      <span>{share(correctCount, answerCount)}</span>
    </Box>
  );
}

const mostChosenWrong = (row: {
  mostChosenWrongAnswer: string | null;
  mostChosenWrongCount: number;
}) =>
  row.mostChosenWrongAnswer === null
    ? "—"
    : `${row.mostChosenWrongAnswer} (${row.mostChosenWrongCount})`;

const sittingStates: Record<SittingRow["state"], string> = {
  running: "u tijeku",
  submitted: "predano",
  expired: "isteklo vrijeme",
};

// What the confirmation dialog is about: closing the Test, or one Student's second Sitting.
type TestAction = { kind: "close" } | { kind: "reset"; sitting: SittingRow };

// label: said beside the name, for a play that was a Sitting of a Public Quiz.
function PlayerName({ name, label }: { name: string; label?: string }) {
  return (
    <TableCell component="th" scope="row">
      {/* The flex box is inside the cell: a cell that is itself flex loses its row's border. */}
      <Box className="quiz-details-player">
        <Initials name={name} small />
        <span>{name}</span>
        {label && <Chip size="small" label={label} />}
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
  // Kept after the dialog closes, so its text does not change while it fades out.
  const [testAction, setTestAction] = useState<TestAction>();
  const [testActionOpen, setTestActionOpen] = useState(false);
  const [testActionRunning, setTestActionRunning] = useState(false);
  const [testActionFailed, setTestActionFailed] = useState(false);

  const askTestAction = (action: TestAction) => {
    setTestAction(action);
    setTestActionFailed(false);
    setTestActionOpen(true);
  };

  const confirmTestAction = async () => {
    if (!testAction || !id) return;
    setTestActionRunning(true);
    const result =
      testAction.kind === "close"
        ? await dispatch(closeTest(parseInt(id)))
        : await dispatch(resetSitting(testAction.sitting.id));
    setTestActionRunning(false);
    if (result.meta.requestStatus === "rejected") {
      setTestActionFailed(true);
      return;
    }
    setTestActionOpen(false);
    dispatch(getQuizAnalytics({ quizId: parseInt(id), keep: true }));
  };

  // How many plays fall in each tenth of the percentage scale, and what they average.
  const { tenthCounts, averagePercent } = useMemo(() => {
    const counts = tenths.map(() => 0);
    let sum = 0;
    let counted = 0;
    (quizGames ?? []).forEach((game) => {
      // A play without answers has no percentage.
      if (game.answers.length === 0) return;
      const percentage = (game.score / game.answers.length) * 100;
      // The last interval includes 100%.
      counts[Math.min(9, Math.floor(percentage / 10))]++;
      sum += percentage;
      counted++;
    });
    return {
      tenthCounts: counts,
      averagePercent: counted > 0 ? Math.round(sum / counted) : undefined,
    };
  }, [quizGames]);

  useEffect(() => {
    if (!id) return;
    const request = dispatch(getQuizAnalytics({ quizId: parseInt(id) }));
    // Leaving for another quiz drops this request, so its late answer cannot replace that quiz's plays.
    return () => request.abort();
  }, [dispatch, id]);

  return (
    <Page>
      <RouterLink className="back-link quiz-details-back" to="/my-quizzes">
        <ArrowBackIcon fontSize="small" /> Moji kvizovi
      </RouterLink>
      <PageHeader
        title="Analitika kviza"
        lead={
          analytics && (
            <>
              <strong className="quiz-details-quiz">{analytics.quizTitle}</strong>
              {quizGames && quizGames.length > 0 && (
                <>
                  {" · "}
                  rješavanja: {quizGames.length} · učenika: {analytics.players.length}
                  {averagePercent !== undefined && ` · prosjek ${averagePercent} %`}
                </>
              )}
            </>
          )
        }
      />
      {analyticsFailed && (
        <Typography className="quiz-game-details-message">
          Analitika nije dostupna.
        </Typography>
      )}
      {analytics?.isTest && (
        <>
          <Paper className="quiz-details-test">
            <Box className="quiz-details-test-facts">
              <Typography>
                Provjera · ulazni kod {analytics.entryCode}
                {analytics.timeLimitMinutes &&
                  ` · ${analytics.timeLimitMinutes} min`}
              </Typography>
              <Typography color="text.secondary">
                {analytics.closesAt === null
                  ? "Bez roka"
                  : analytics.isClosed
                  ? `Zatvorena ${formatDateTime(analytics.closesAt)}`
                  : `Zatvara se: ${formatDateTime(analytics.closesAt)}`}
              </Typography>
            </Box>
            {!analytics.isClosed && (
              <Button
                variant="outlined"
                onClick={() => askTestAction({ kind: "close" })}
              >
                Zatvori provjeru
              </Button>
            )}
          </Paper>
          <Typography variant="h5" component="h2" className="quiz-details-heading">
            Pokušaji
          </Typography>
          {analytics.sittings.length === 0 ? (
            <Typography className="quiz-game-details-message">
              Još nitko nije započeo provjeru.
            </Typography>
          ) : (
            <TableContainer component={Paper}>
              <Table aria-label="Pokušaji">
                <TableHead>
                  <TableRow>
                    <TableCell>Učenik</TableCell>
                    <TableCell align="right">Početak</TableCell>
                    <TableCell>Stanje</TableCell>
                    <TableCell align="right">Rezultat</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {analytics.sittings.map((sitting) => (
                    <TableRow key={sitting.id}>
                      <PlayerName name={sitting.userName} />
                      <TableCell align="right">
                        {formatDateTime(sitting.startedAt)}
                      </TableCell>
                      <TableCell>{sittingStates[sitting.state]}</TableCell>
                      <TableCell align="right">
                        {/* The flex box is inside the cell, as in PlayerName. */}
                        <Box className="quiz-details-sitting">
                          {sitting.score !== null && sitting.maxScore !== null
                            ? `${sitting.score} / ${sitting.maxScore} ${pointsLabel(
                                sitting.maxScore
                              )}`
                            : "—"}
                          <Button
                            size="small"
                            onClick={() =>
                              askTestAction({ kind: "reset", sitting })
                            }
                          >
                            Dopusti ponovno
                          </Button>
                        </Box>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </TableContainer>
          )}
          <Dialog
            open={testActionOpen}
            onClose={() => setTestActionOpen(false)}
          >
            <DialogTitle>
              {testAction?.kind === "reset" ? "Novi pokušaj" : "Zatvaranje provjere"}
            </DialogTitle>
            <DialogContent>
              <DialogContentText>
                {testAction?.kind === "reset"
                  ? `Pokušaj učenika ${testAction.sitting.userName} i njegov rezultat bit će trajno obrisani.`
                  : `Pokušaji u tijeku (${
                      analytics.sittings.filter(
                        (sitting) => sitting.state === "running"
                      ).length
                    }) bit će predani s dosad spremljenim odgovorima. Učenici će nakon toga vidjeti točne odgovore.`}
              </DialogContentText>
              {testActionFailed && (
                <Typography color="error" sx={{ marginTop: 1 }}>
                  Radnja nije uspjela. Pokušajte ponovno.
                </Typography>
              )}
            </DialogContent>
            <DialogActions>
              <Button onClick={() => setTestActionOpen(false)}>Odustani</Button>
              <Button
                color={testAction?.kind === "reset" ? "error" : "primary"}
                disabled={testActionRunning}
                onClick={() => confirmTestAction()}
              >
                {testAction?.kind === "reset" ? "Obriši" : "Zatvori"}
              </Button>
            </DialogActions>
          </Dialog>
        </>
      )}
      {quizGames && quizGames.length === 0 && (
        <EmptyState icon={<BarChartOutlinedIcon />} title="Još nitko nije riješio ovaj kviz.">
          Kad ga učenici riješe, ovdje ćete vidjeti uspjeh po pitanjima, najčešće
          pogreške i rezultat svakog učenika.
        </EmptyState>
      )}
      {quizGames && quizGames.length > 0 && (
        <>
          <Typography variant="h5" component="h2" className="quiz-details-heading">
            Raspodjela rezultata
          </Typography>
          <Paper className="quiz-details-chart">
            <Typography className="quiz-details-note">
              Broj rješavanja prema postotku osvojenih bodova.
            </Typography>
            <BarChart
              height={220}
              margin={{ top: 12, right: 12, bottom: 28, left: 36 }}
              borderRadius={4}
              xAxis={[{ scaleType: "band", data: tenths }]}
              yAxis={[{ tickMinStep: 1 }]}
              series={[{ data: tenthCounts, color: "#553b08" }]}
            />
          </Paper>
        </>
      )}
      {analytics && analytics.games.length > 0 && (
        <>
          <Typography variant="h5" component="h2" className="quiz-details-heading">
            Uspjeh po pitanjima
          </Typography>
          <TableContainer component={Paper}>
            <Table aria-label="Uspjeh po pitanjima">
              <TableHead>
                <TableRow>
                  <TableCell>Pitanje</TableCell>
                  <TableCell align="right">Točni odgovori</TableCell>
                  <TableCell>Najčešći pogrešan odgovor</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {analytics.questions.map((question, index) => (
                  <Fragment key={question.questionId}>
                    {/* A heading above the first of the Questions asked about one Source Text. */}
                    {question.sourceTextId !== null &&
                      question.sourceTextId !==
                        analytics.questions[index - 1]?.sourceTextId && (
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
                            className="quiz-details-lines"
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
                        <Share
                          correctCount={question.correctCount}
                          answerCount={question.answerCount}
                        />
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
          <Typography variant="h5" component="h2" className="quiz-details-heading">
            Učenici
          </Typography>
          <TableContainer component={Paper}>
            <Table aria-label="Učenici">
              <TableHead>
                <TableRow>
                  <TableCell>Ime i prezime</TableCell>
                  <TableCell align="right">Broj rješavanja</TableCell>
                  <TableCell align="right">Prvi rezultat</TableCell>
                  <TableCell align="right">Najbolji rezultat</TableCell>
                  <TableCell align="right">Zadnje rješavanje</TableCell>
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
          <Typography variant="h5" component="h2" className="quiz-details-heading">
            Sva rješavanja
          </Typography>
          <TableContainer component={Paper}>
            <Table aria-label="Sva rješavanja">
              <TableHead>
                <TableRow>
                  <TableCell />
                  <TableCell>Ime i prezime</TableCell>
                  <TableCell align="right">Datum rješavanja</TableCell>
                  <TableCell align="right">Ostvareni rezultat</TableCell>
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
                        <PlayerName
                          name={quizGame.userName}
                          label={
                            quizGame.playedAs === "mock"
                              ? "Kao provjera"
                              : undefined
                          }
                        />
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
    </Page>
  );
}
