import { useEffect } from "react";
import { useSelector } from "react-redux";
import { useNavigate, useParams } from "react-router-dom";
import { Box, Button, Paper, Typography } from "@mui/material";
import { RootState, useAppDispatch } from "../../../store/store";
import { fetchQuizGame } from "../../../store/slices/quizSlice";
import { pointsLabel } from "../../../types/points";
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
  const { gameReview: storedReview, gameReviewFailed } = useSelector(
    (state: RootState) => state.quiz
  );
  // The store may still hold the play opened before this one until the fetch below starts.
  const gameReview = storedReview?.id === Number(id) ? storedReview : undefined;
  const dispatch = useAppDispatch();
  const navigate = useNavigate();

  useEffect(() => {
    const request = dispatch(fetchQuizGame(Number(id)));
    // Leaving for another play drops this request, so its late answer cannot replace that play.
    return () => request.abort();
  }, [dispatch, id]);

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
          {gameReview.answers.map((answer, index) => {
            const correct = answer.answerText === answer.correctAnswer;
            return (
              <Paper key={answer.id} className="game-review-answer">
                <Typography variant="h6">
                  {index + 1}. {answer.questionTitle}
                </Typography>
                <Typography
                  className={
                    correct ? "game-review-correct" : "game-review-wrong"
                  }
                >
                  Vaš odgovor: {answer.answerText}
                </Typography>
                {!correct && (
                  <Typography>Točan odgovor: {answer.correctAnswer}</Typography>
                )}
                {answer.exploreMore && (
                  <Box className="game-review-explore">
                    <Typography variant="subtitle2">Saznaj više</Typography>
                    <Typography>{answer.exploreMore}</Typography>
                  </Box>
                )}
              </Paper>
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
