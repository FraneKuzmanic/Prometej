import { useParams } from "react-router-dom";
import { useEffect, useState } from "react";
import { RootState, useAppDispatch } from "../../store/store";
import { useSelector } from "react-redux";
import { getQuizAnalytics } from "../../store/slices/quizSlice";
import {
  Avatar,
  Box,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Typography,
} from "@mui/material";
import { QuizGameViewModel } from "../../types/models/Quiz";
import "./styles.css";
import { stringToColor } from "../../components/QuizContainer";
import { PieChart } from "@mui/x-charts/PieChart";

interface SeriesData {
  id: number;
  value: number;
  label: string;
}

export function QuizDetails() {
  const { id } = useParams();
  const { quizGames } = useSelector((state: RootState) => state.quiz);
  const dispatch = useAppDispatch();
  const [seriesData, setSeriesData] = useState<SeriesData[]>([]);

  useEffect(() => {
    setChart();
  }, [quizGames]);

  useEffect(() => {
    if (id) {
      dispatch(getQuizAnalytics(parseInt(id)));
    }
  }, []);

  const formatDate = (date: Date) => {
    const options: Intl.DateTimeFormatOptions = {
      year: "numeric",
      month: "long",
      day: "numeric",
    };
    return new Date(date).toLocaleDateString("hr-HR", options);
  };

  const setChart = () => {
    if (quizGames) {
      const intervals = Array.from({ length: 10 }, (_, i) => i * 10);
      const intervalCounts = intervals.map((_, i) => {
        const lowerBound = i * 10;
        const upperBound = (i + 1) * 10;
        return quizGames.filter(
          (game) =>
            (game.score / game.answers.length) * 100 >= lowerBound &&
            (game.score / game.answers.length) * 100 < upperBound
        ).length;
      });
      const seriesData: SeriesData[] = intervals.map((interval, i) => ({
        id: i,
        value: intervalCounts[i],
        label: `${interval}% - ${interval + 10}%`,
      }));

      setSeriesData(seriesData);
    }
  };
  return (
    <Box className="quiz-game-details-wrapper">
      <Typography variant="h3" className="quiz-game-details-title">
        Analitika kviza
      </Typography>
      <Box className="pie-chart-container">
        <PieChart
          series={[
            {
              data: [
                { id: 1, value: 2, label: "40%-50%" },
                { id: 2, value: 1, label: "90%-100%" },
              ],
            },
          ]}
          width={400}
          height={200}
        />
      </Box>
      {quizGames && (
        <TableContainer component={Paper}>
          <Table aria-label="simple table">
            <TableHead>
              <TableRow>
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
              {quizGames.map((quizGame: QuizGameViewModel) => (
                <TableRow
                  key={quizGame.id}
                  sx={{ "&:last-child td, &:last-child th": { border: 0 } }}
                >
                  <TableCell
                    sx={{ display: "flex", alignItems: "center" }}
                    component="th"
                    scope="row"
                  >
                    <Avatar sx={{ bgcolor: stringToColor(quizGame.userName) }}>
                      {quizGame.userName[0].toUpperCase()}
                    </Avatar>
                    <Typography
                      sx={{ marginLeft: 1.5, fontSize: 14 }}
                      gutterBottom
                      variant="h6"
                      component="div"
                    >
                      {quizGame.userName}
                    </Typography>
                  </TableCell>
                  <TableCell align="right">
                    {formatDate(quizGame.datePlayed)}
                  </TableCell>
                  <TableCell align="right">
                    {quizGame.score} / {quizGame.answers.length}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </TableContainer>
      )}
    </Box>
  );
}
