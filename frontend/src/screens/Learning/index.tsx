import { useEffect } from "react";
import { Typography } from "@mui/material";
import ArrowForwardIcon from "@mui/icons-material/ArrowForward";
import { Link } from "react-router-dom";
import { useSelector } from "react-redux";
import { RootState, useAppDispatch } from "../../store/store";
import { fetchPeriods } from "../../store/slices/periodSlice";
import { Page, PageHeader } from "../../components/Page";
import "./styles.css";

export default function Learning() {
  const dispatch = useAppDispatch();
  const { periods, periodsFailed } = useSelector(
    (state: RootState) => state.period
  );

  useEffect(() => {
    dispatch(fetchPeriods());
  }, [dispatch]);

  if (!periods) {
    return periodsFailed ? (
      <Typography>Razdoblja se nisu učitala. Pokušajte ponovno.</Typography>
    ) : null;
  }

  return (
    <Page>
      <PageHeader
        title="Književna razdoblja"
        lead="Gradivo hrvatske i svjetske književnosti po razdobljima, redom kojim se uče."
      />
      <ul className="period-grid">
        {periods.map((period) => (
          <li key={period.id}>
            {/* The whole card opens the Period; "Pregled" only says so. */}
            <Link className="period-card" to={`/learning/${period.id}`}>
              {period.image && (
                <span className="period-card-image">
                  <img src={`/${period.image}`} alt="" />
                </span>
              )}
              <span className="period-card-text">
                <Typography variant="h5" component="h2">
                  {period.name}
                </Typography>
                <Typography component="span" className="period-card-description">
                  {period.description}
                </Typography>
                <span className="period-card-open">
                  Pregled <ArrowForwardIcon fontSize="small" />
                </span>
              </span>
            </Link>
          </li>
        ))}
      </ul>
    </Page>
  );
}
