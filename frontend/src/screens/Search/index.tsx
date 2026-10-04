import { useEffect } from "react";
import { useLocation, useNavigate, useSearchParams } from "react-router-dom";
import { RootState, useAppDispatch } from "../../store/store";
import { useSelector } from "react-redux";
import { Box, Button, Typography } from "@mui/material";
import { searchPeriodContent } from "../../store/slices/periodSlice";
import "./styles.css";

export default function SearchContent() {
  const navigate = useNavigate();
  const dispatch = useAppDispatch();
  const { searchContent, searchFailed } = useSelector(
    (state: RootState) => state.period
  );
  // The query is in the address, so a search can be reloaded or sent to someone.
  const [searchParams] = useSearchParams();
  const query = (searchParams.get("q") ?? "").trim();
  const tooShort = query.length < 2;
  // Changes on every navigation, so Enter on the same query searches again.
  const { key } = useLocation();

  useEffect(() => {
    if (tooShort) return;
    const request = dispatch(searchPeriodContent(query));
    // A newer search drops this request, so its late answer cannot replace the newer results.
    return () => request.abort();
  }, [dispatch, query, tooShort, key]);

  if (tooShort) {
    return (
      <Typography>Upišite najmanje dva znaka za pretraživanje.</Typography>
    );
  }
  if (searchFailed) {
    return <Typography>Pretraživanje nije uspjelo. Pokušajte ponovno.</Typography>;
  }
  if (!searchContent) {
    return null;
  }
  if (searchContent.length === 0) {
    return <Typography>Nema rezultata za „{query}”.</Typography>;
  }

  return (
    <Box sx={{ width: "100%" }}>
      {searchContent.map((content) => (
        <Box key={content.periodId}>
          <Typography variant="h5" component="h2" gutterBottom>
            {content.periodName}
          </Typography>
          {content.passages.map((passage, index) => (
            <Box className="search-passage" key={index}>
              {/* A Period Content usually opens with its Period's name as a heading. */}
              {passage.heading && passage.heading !== content.periodName && (
                <Typography variant="h6" component="h3">
                  {passage.heading}
                </Typography>
              )}
              <Typography>{passage.text}</Typography>
            </Box>
          ))}
          {content.matchCount > content.passages.length && (
            <Typography variant="body2" color="text.secondary">
              Broj podudaranja u ovom razdoblju: {content.matchCount}.
            </Typography>
          )}
          <Button
            onClick={() => navigate(`/learning/${content.periodId}`)}
            variant="text"
            className="more-of-button"
          >
            Više o...
          </Button>
          <hr></hr>
        </Box>
      ))}
    </Box>
  );
}
