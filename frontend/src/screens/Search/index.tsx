import { useNavigate } from "react-router-dom";
import { RootState } from "../../store/store";
import { useSelector } from "react-redux";
import { Box, Button } from "@mui/material";
import { PeriodSearchContent } from "../../types/models/Period";
import "./styles.css";

export default function SearchContent() {
  const navigate = useNavigate();
  const { searchContent } = useSelector((state: RootState) => state.period);

  return (
    <Box sx={{ width: "100%" }}>
      {searchContent?.map((content: PeriodSearchContent) => (
        <Box key={content.periodId}>
          <Box
            className="content"
            dangerouslySetInnerHTML={{
              __html: content.searchContent,
            }}
          />
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
