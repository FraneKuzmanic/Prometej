import { ReactNode, useCallback, useEffect, useState } from "react";
import { useSelector } from "react-redux";
import {
  Link as RouterLink,
  useNavigate,
  useParams,
  useSearchParams,
} from "react-router-dom";
import { Box, Button, Link, Paper, Typography } from "@mui/material";
import { RootState, useAppDispatch } from "../../../store/store";
import {
  discardSitting,
  fetchSittingInfo,
  startSitting,
} from "../../../store/slices/sittingSlice";
import { SittingInfo, SittingResult } from "../../../types/models/Sitting";
import { pointsLabel } from "../../../types/points";
import { formatDateTime } from "../../Discussion/format";
import Questions from "./Questions";
import "../styles.css";
import "./styles.css";

// "1 pitanje", "21 pitanje", and "pitanja" for every other number.
const questionsLabel = (count: number) =>
  `${count} ${count % 10 === 1 && count % 100 !== 11 ? "pitanje" : "pitanja"}`;

// A Quiz solved under test conditions: a Test opened with its Entry Code, or a Public Quiz
// the user chose to solve this way. The server holds the Questions' answers, the user's
// answers and the clock; this screen shows the start, the Questions and the result.
export default function SittingScreen() {
  const { quizId } = useParams();
  const [searchParams] = useSearchParams();
  const code = searchParams.get("code") ?? undefined;
  const { authenticated } = useSelector((state: RootState) => state.user);
  const {
    info: storedInfo,
    infoFailed,
    requestedQuizId,
    sitting,
    result,
  } = useSelector((state: RootState) => state.sitting);
  // The store may still hold another Quiz until the fetch below starts.
  const isRequested = requestedQuizId === Number(quizId);
  const info = isRequested ? storedInfo : undefined;
  const dispatch = useAppDispatch();
  const navigate = useNavigate();
  const [endedByServer, setEndedByServer] = useState(false);
  const [discardedByEdit, setDiscardedByEdit] = useState(false);
  const [startFailed, setStartFailed] = useState(false);
  const [discarding, setDiscarding] = useState(false);

  useEffect(() => {
    if (!authenticated) return;
    setEndedByServer(false);
    setDiscardedByEdit(false);
    setStartFailed(false);
    const request = dispatch(fetchSittingInfo({ quizId: Number(quizId), code }));
    return () => request.abort();
  }, [dispatch, quizId, code, authenticated]);

  const handleEndedByServer = useCallback(() => setEndedByServer(true), []);
  const handleDiscardedByEdit = useCallback(() => setDiscardedByEdit(true), []);

  const start = async () => {
    setStartFailed(false);
    const started = await dispatch(startSitting({ quizId: Number(quizId), code }));
    if (startSitting.rejected.match(started)) {
      // Closed, or sat already, since the start screen was read: it says which.
      if (started.payload === 409 || started.payload === 404) {
        dispatch(fetchSittingInfo({ quizId: Number(quizId), code }));
      } else {
        setStartFailed(true);
      }
    }
  };

  // A running Sitting of a Public Quiz is given up from the start screen. Its id is the
  // server's, so it is asked for the way a resume asks for it.
  const discard = async () => {
    // Nothing is shown meanwhile: the Sitting is in the store for a moment, only to be dropped.
    setDiscarding(true);
    const running = await dispatch(startSitting({ quizId: Number(quizId), code }));
    if (startSitting.fulfilled.match(running)) {
      await dispatch(discardSitting(running.payload.id));
    }
    dispatch(fetchSittingInfo({ quizId: Number(quizId), code }));
    setDiscarding(false);
  };

  const home = (
    <Button onClick={() => navigate("/")} variant="contained">
      Vrati na početnu
    </Button>
  );

  const panel = (title: string, body: ReactNode, actions: ReactNode = home) => (
    <Paper elevation={3} className="quiz-play-container">
      <Box className="quiz-play-header">
        <Typography className="quiz-play-title" variant="h5">
          {title}
        </Typography>
      </Box>
      <Box className="sitting-panel">{body}</Box>
      <Box className="quiz-play-footer" sx={{ justifyContent: "flex-end", gap: 1 }}>
        {actions}
      </Box>
    </Paper>
  );

  const resultPanel = (
    quiz: SittingInfo,
    ended: SittingResult,
    alreadySat: boolean
  ) =>
    panel(
      quiz.title,
      <>
        {alreadySat && <Typography>Ovu ste provjeru već riješili.</Typography>}
        {endedByServer && (
          <Typography>Vrijeme je isteklo. Vaši odgovori su predani.</Typography>
        )}
        <Typography component="div" variant="h4" className="sitting-score">
          Vaš rezultat: {ended.score} / {ended.maxScore}{" "}
          {pointsLabel(ended.maxScore)}
        </Typography>
        {!ended.reviewAvailable && (
          <Typography>Odgovori će biti vidljivi kad provjera završi.</Typography>
        )}
      </>,
      <>
        {ended.reviewAvailable && (
          <Button onClick={() => navigate(`/my-results/${ended.gameId}`)}>
            Pregledaj odgovore
          </Button>
        )}
        {home}
      </>
    );

  const startPanel = (quiz: SittingInfo) => {
    const mayStart = !quiz.isOwn && (!quiz.isClosed || quiz.running);
    return panel(
      quiz.title,
      <>
        <Typography variant="overline" className="sitting-eyebrow">
          {quiz.isTest ? "Provjera" : "Kao provjera"}
        </Typography>
        <Typography>
          {questionsLabel(quiz.questionCount)} · {quiz.maxScore}{" "}
          {pointsLabel(quiz.maxScore)}
        </Typography>
        <Typography>
          {quiz.timeLimitMinutes
            ? `Vrijeme: ${quiz.timeLimitMinutes} min`
            : "Bez vremenskog ograničenja"}
        </Typography>
        {quiz.closesAt && (
          <Typography>Zatvara se: {formatDateTime(quiz.closesAt)}</Typography>
        )}
        {quiz.isTest && <Typography>Imate jedan pokušaj.</Typography>}
        <Typography>
          Odgovori se spremaju dok rješavate. Točni odgovori vidljivi su nakon
          završetka.
        </Typography>
        {quiz.isOwn && (
          <Typography className="sitting-note">
            Ovo je vaš kviz, pa ga ne možete rješavati kao provjeru.
          </Typography>
        )}
        {!quiz.isOwn && quiz.isClosed && !quiz.running && (
          <Typography className="sitting-note">Provjera je zatvorena.</Typography>
        )}
        {startFailed && (
          <Typography color="error" className="sitting-note">
            Provjera se nije pokrenula. Pokušajte ponovno.
          </Typography>
        )}
      </>,
      <>
        {home}
        {quiz.running && !quiz.isTest && (
          <Button variant="outlined" onClick={() => discard()}>
            Odustani
          </Button>
        )}
        {mayStart && (
          <Button variant="contained" onClick={() => start()}>
            {quiz.running ? "Nastavi" : "Započni"}
          </Button>
        )}
      </>
    );
  };

  const content = () => {
    // Nothing until the session is known: a signed-in user must not see the sign-in line.
    if (authenticated === undefined) return null;
    if (!authenticated) {
      return panel(
        "Provjera",
        <Typography>
          <Link component={RouterLink} to="/login">
            Prijavite se
          </Link>{" "}
          da biste riješili provjeru.
        </Typography>
      );
    }
    if (isRequested && infoFailed) {
      return panel(
        "Provjera",
        <Typography>Provjera se nije učitala. Pokušajte ponovno.</Typography>
      );
    }
    if (info === undefined || discarding) return null;
    if (info === null) {
      return panel("Provjera", <Typography>Provjera ne postoji.</Typography>);
    }
    if (discardedByEdit) {
      return panel(
        info.title,
        <Typography>
          Kviz je u međuvremenu izmijenjen, pa je pokušaj prekinut.
        </Typography>
      );
    }
    if (result) return resultPanel(info, result, false);
    if (sitting) {
      return (
        <Questions
          key={sitting.id}
          sitting={sitting}
          onEndedByServer={handleEndedByServer}
          onDiscardedByEdit={handleDiscardedByEdit}
        />
      );
    }
    if (info.result) return resultPanel(info, info.result, true);
    return startPanel(info);
  };

  return <Box className="play-quiz-screen-wrapper">{content()}</Box>;
}
