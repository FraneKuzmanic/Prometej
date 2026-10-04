import { useEffect } from "react";
import { styled } from "@mui/material/styles";
import Paper from "@mui/material/Paper";
import Box from "@mui/material/Box";
import { Button, Typography, Unstable_Grid2 as Grid } from "@mui/material";
import "./styles.css";
import ArrowForwardIcon from "@mui/icons-material/ArrowForward";
import { useNavigate } from "react-router-dom";
import { useSelector } from "react-redux";
import { RootState, useAppDispatch } from "../../store/store";
import { fetchPeriods } from "../../store/slices/periodSlice";

const Item = styled(Paper)(({ theme }) => ({
  backgroundColor: theme.palette.mode === "dark" ? "#1A2027" : "#fff",
  ...theme.typography.body2,
  padding: theme.spacing(0),
  color: theme.palette.text.secondary,
}));

export default function Learning() {
  const navigate = useNavigate();
  const dispatch = useAppDispatch();
  const { periods, periodsFailed } = useSelector(
    (state: RootState) => state.period
  );

  useEffect(() => {
    dispatch(fetchPeriods());
  }, [dispatch]);

  const openLiteraryPeriod = (id: number) => {
    navigate(`/learning/${id}`);
  };

  if (!periods) {
    return periodsFailed ? (
      <Typography>Razdoblja se nisu učitala. Pokušajte ponovno.</Typography>
    ) : null;
  }

  return (
    <Box sx={{ width: "100%" }}>
      <Grid container rowSpacing={10} columnSpacing={{ xs: 1, sm: 2, md: 3 }}>
        {periods.map((period) => (
          <Grid xs={6} key={period.id}>
            <Item className="item">
              <Box sx={{ padding: 2, flex: 1 }}>
                <Typography variant="h4">{period.name}</Typography>
                <Typography variant="subtitle2">{period.timeFrame}</Typography>
                <Typography variant="body1">{period.description}</Typography>
                <Button
                  onClick={() => openLiteraryPeriod(period.id)}
                  className="button"
                  variant="text"
                >
                  Idi na <ArrowForwardIcon style={{ marginLeft: 4 }} />
                </Button>
              </Box>
              {period.image ? (
                <Box className="img-container">
                  <img className="img" src={`/${period.image}`} alt="" />
                </Box>
              ) : (
                <Box className="img-container img-placeholder">
                  <span>{period.timeFrame}</span>
                </Box>
              )}
            </Item>
          </Grid>
        ))}
      </Grid>
    </Box>
  );
}
