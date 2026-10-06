import { Fragment, useEffect } from "react";
import { useSelector } from "react-redux";
import { useNavigate, useParams } from "react-router-dom";
import { Box, Button, Paper, Typography } from "@mui/material";
import { RootState, useAppDispatch } from "../../../store/store";
import { fetchQuizGame } from "../../../store/slices/quizSlice";
import { pointsLabel } from "../../../types/points";
import AnswerGroup from "../../../components/AnswerGroup";
import { groupAnswers } from "../../../components/AnswerGroup/group";
import "../styles.css";

const formatDate = (date: string) =>
  new Date(date).toLocaleDateString("hr-HR", {
    year: "numeric",
    month: "long",
    day: "numeric",
  });

// One of the player's own Quiz Games, read again: every Question as it was when played.
export default function GameReview() {
  const { id } = useParams();
  const {
    gameReview: storedReview,
    gameReviewFailed,
    gameReviewLocked,
  } = useSelector((state: RootState) => state.quiz);
  // The store may still hold the play opened before this one until the fetch below starts.
  const gameReview = storedReview?.id === Number(id) ? storedReview : undefined;
  const dispatch = useAppDispatch();
  const navigate = useNavigate();

  useEffect(() => {
    const request = dispatch(fetchQuizGame(Number(id)));
    // Leaving for another play drops this request, so its late answer cannot replace that play.
    return () => request.abort();
  }, [dispatch, id]);

  const groups = groupAnswers(gameReview?.answers ?? []);

  const backToResults = (
    <Button onClick={() => navigate("/my-results")}>Natrag na rezultate</Button>
  );

  return (
    <Box className="my-results-wrapper">
      {gameReviewFailed && (
        <Box>
          <Typography>Rezultat ne postoji.</Typography>
          {backToResults}
        </Box>
      )}
      {gameReviewLocked && (
        <Box>
          <Typography>Odgovori će biti vidljivi kad provjera završi.</Typography>
          {backToResults}
        </Box>
      )}
      {gameReview && (
        <>
          <Typography variant="h4" className="my-results-title">
            {gameReview.quizTitle}
          </Typography>
          <Typography>
            {formatDate(gameReview.datePlayed)} · {gameReview.score} /{" "}
            {gameReview.answers.length} {pointsLabel(gameReview.answers.length)}
            {gameReview.periodName && ` · ${gameReview.periodName}`}
          </Typography>
          {groups.map((answers, index) => {
            const { id, sourceTextId } = answers[0];
            // Shown once, above the first of the Questions that were asked about it.
            const sourceText =
              sourceTextId !== null &&
              sourceTextId !== groups[index - 1]?.[0].sourceTextId
                ? gameReview.sourceTexts.find((text) => text.id === sourceTextId)
                : undefined;
            return (
              <Fragment key={id}>
                {sourceText && (
                  <details className="game-review-source">
                    <summary>Polazni tekst</summary>
                    <Typography variant="subtitle1">
                      {sourceText.caption}
                    </Typography>
                    <Typography className="game-review-source-body">
                      {sourceText.body}
                    </Typography>
                  </details>
                )}
                <Paper className="game-review-answer">
                  <AnswerGroup
                    number={index + 1}
                    answers={answers}
                    answerLabel="Vaš odgovor"
                    showExploreMore
                  />
                </Paper>
              </Fragment>
            );
          })}
          <Box className="game-review-footer">
            {gameReview.quizIsListed && (
              <Button
                variant="contained"
                sx={{ backgroundColor: "#553b08" }}
                onClick={() => navigate(`/play-quiz/${gameReview.quizId}`)}
              >
                Igraj ponovno
              </Button>
            )}
            {gameReview.periodId && (
              <Button
                variant="outlined"
                onClick={() => navigate(`/learning/${gameReview.periodId}`)}
              >
                Ponovi gradivo: {gameReview.periodName}
              </Button>
            )}
            {backToResults}
          </Box>
        </>
      )}
    </Box>
  );
}
