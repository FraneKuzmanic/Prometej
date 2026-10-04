import Card from "@mui/material/Card";
import CardContent from "@mui/material/CardContent";
import CardHeader from "@mui/material/CardHeader";
import Typography from "@mui/material/Typography";
import { Avatar, CardActionArea } from "@mui/material";
import { stringToColor } from "./stringToColor";

interface Props {
  name: string;
  authorName: string;
  entryCode?: number;
  periodName: string | null;
  questionCount: number;
}

// "1 pitanje", "21 pitanje", and "pitanja" for every other number.
const questionsLabel = (count: number) =>
  `${count} ${count % 10 === 1 && count % 100 !== 11 ? "pitanje" : "pitanja"}`;

export default function QuizContainer(props: Props) {
  return (
    <Card>
      <CardActionArea>
        <CardHeader
          title={<Typography variant="h6">{props.name}</Typography>}
          subheader={[props.periodName, questionsLabel(props.questionCount)]
            .filter(Boolean)
            .join(" · ")}
        />
        <CardContent
          sx={{
            display: "flex",
            alignItems: "center",
            marginTop: 4,
            position: "relative",
          }}
        >
          <Avatar sx={{ bgcolor: stringToColor(props.authorName) }}>
            {props.authorName[0].toUpperCase()}
          </Avatar>
          <Typography
            sx={{ marginLeft: 1.5, fontSize: 14 }}
            gutterBottom
            variant="h6"
            component="div"
          >
            {props.authorName}
          </Typography>
          {props.entryCode && (
            <Typography
              variant="h6"
              sx={{
                fontSize: 14,
                position: "absolute",
                bottom: 0,
                right: 0,
                padding: 1,
              }}
            >
              Ulazni kod:
              <span
                style={{ color: "#553b08", marginLeft: 5, fontStyle: "italic" }}
              >
                {props.entryCode}
              </span>
            </Typography>
          )}
        </CardContent>
      </CardActionArea>
    </Card>
  );
}
