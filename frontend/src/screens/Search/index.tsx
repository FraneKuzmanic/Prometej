import { ReactNode, useEffect } from "react";
import { Link as RouterLink, useLocation, useSearchParams } from "react-router-dom";
import { RootState, useAppDispatch } from "../../store/store";
import { useSelector } from "react-redux";
import { Box, Paper, Typography } from "@mui/material";
import ArrowForwardIcon from "@mui/icons-material/ArrowForward";
import SearchIcon from "@mui/icons-material/Search";
import SearchOffIcon from "@mui/icons-material/SearchOff";
import { fetchPeriods, searchPeriodContent } from "../../store/slices/periodSlice";
import { EmptyState, Page, PageHeader } from "../../components/Page";
import "./styles.css";

// As the server compares: without case and without diacritics, "đ" read as "d". One
// character stays one character, so a place found in the folded text is the same place in
// the text itself.
const fold = (text: string) =>
  Array.from(text, (char) => {
    const folded = char.normalize("NFD")[0].toLowerCase().replace("đ", "d");
    return folded.length === char.length ? folded : char;
  }).join("");

// The passage with every place the query was found at marked.
const highlight = (text: string, query: string): ReactNode => {
  const foldedText = fold(text);
  const foldedQuery = fold(query);
  // A fold that changed a length would mark the wrong letters; then nothing is marked.
  if (foldedQuery === "" || foldedText.length !== text.length) return text;
  const parts: ReactNode[] = [];
  let from = 0;
  for (;;) {
    const at = foldedText.indexOf(foldedQuery, from);
    if (at === -1) break;
    parts.push(text.slice(from, at));
    parts.push(<mark key={at}>{text.slice(at, at + foldedQuery.length)}</mark>);
    from = at + foldedQuery.length;
  }
  parts.push(text.slice(from));
  return parts;
};

export default function SearchContent() {
  const dispatch = useAppDispatch();
  const { searchContent, searchFailed, periods } = useSelector(
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

  // A result shows its Period's painting.
  useEffect(() => {
    if (!periods) dispatch(fetchPeriods());
  }, [dispatch, periods]);

  if (tooShort) {
    return (
      <Page narrow>
        <EmptyState icon={<SearchIcon />} title="Pretražite gradivo">
          Upišite najmanje dva znaka za pretraživanje.
        </EmptyState>
      </Page>
    );
  }
  if (searchFailed) {
    return (
      <Page narrow>
        <EmptyState
          icon={<SearchOffIcon />}
          title="Pretraživanje nije uspjelo. Pokušajte ponovno."
        />
      </Page>
    );
  }
  if (!searchContent) {
    return null;
  }
  if (searchContent.length === 0) {
    return (
      <Page narrow>
        <EmptyState icon={<SearchOffIcon />} title={`Nema rezultata za „${query}”.`}>
          Pretraga traži točno te riječi u gradivu, bez obzira na velika slova i
          dijakritike. Pokušajte s kraćim pojmom ili samo s prezimenom autora.
        </EmptyState>
      </Page>
    );
  }

  return (
    <Page narrow>
      <PageHeader title={`Rezultati za „${query}”`} />
      {searchContent.map((content) => {
        const image = periods?.find((period) => period.id === content.periodId)?.image;
        return (
          <Paper className="search-result" key={content.periodId}>
            <Box className="search-result-head">
              {image && <img src={`/${image}`} alt="" />}
              <Typography variant="h5" component="h2">
                {content.periodName}
              </Typography>
            </Box>
            {content.passages.map((passage, index) => (
              <Box className="search-passage" key={index}>
                {/* A Period Content usually opens with its Period's name as a heading. */}
                {passage.heading && passage.heading !== content.periodName && (
                  <Typography variant="h6" component="h3">
                    {passage.heading}
                  </Typography>
                )}
                <Typography>{highlight(passage.text, query)}</Typography>
              </Box>
            ))}
            <Box className="search-result-foot">
              {content.matchCount > content.passages.length && (
                <Typography variant="body2" color="text.secondary">
                  Broj podudaranja u ovom razdoblju: {content.matchCount}.
                </Typography>
              )}
              <RouterLink
                to={`/learning/${content.periodId}`}
                className="more-of-button"
              >
                Više o razdoblju <ArrowForwardIcon fontSize="small" />
              </RouterLink>
            </Box>
          </Paper>
        );
      })}
    </Page>
  );
}
