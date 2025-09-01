import { Box, Checkbox, Grid, TextField, Typography } from "@mui/material";
import "./styles.css";
import { QuestionCreateRequest } from "../../types/models/Quiz";
import RadioButtonUncheckedIcon from "@mui/icons-material/RadioButtonUnchecked";
import CheckCircleIcon from "@mui/icons-material/CheckCircle";

interface QuestionContainerProps {
  selected: number;
  currentQuestion?: QuestionCreateRequest;
  handleQuestionChange: (
    questionNo: number,
    updates: Partial<QuestionCreateRequest>
  ) => void;
}

export default function QuestionContainer({
  currentQuestion,
  handleQuestionChange,
  selected,
}: QuestionContainerProps) {
  const handleCheckboxChange = (
    e: React.ChangeEvent<HTMLInputElement>,
    answer?: string
  ) => {
    if (e.target.checked && answer) {
      handleQuestionChange(selected, { correctAnswer: answer });
    } else {
      handleQuestionChange(selected, { correctAnswer: "" });
    }
  };

  return (
    <Box className="question-wrapper">
      <Box className="question-title">
        <TextField
          className="input-title"
          id="outlined-basic"
          label="Unesite pitanje"
          multiline
          value={currentQuestion ? currentQuestion.questionTitle : ""}
          onChange={(e) =>
            handleQuestionChange(selected, { questionTitle: e.target.value })
          }
        />
      </Box>
      <Grid
        className="answers-grid"
        container
        rowSpacing={4}
        columnSpacing={{ xs: 1, sm: 2, md: 3 }}
      >
        <Grid className="answer" item xs={6}>
          <Typography className="letter" component="span">
            A
          </Typography>
          <TextField
            className="input"
            id="outlined-basic"
            label="Unesite odgovor"
            value={currentQuestion ? currentQuestion.firstAnswer : ""}
            onChange={(e) =>
              handleQuestionChange(selected, { firstAnswer: e.target.value })
            }
            multiline
          />
          <Checkbox
            className="question-checkbox"
            {...{ inputProps: { "aria-label": "Checkbox demo" } }}
            icon={<RadioButtonUncheckedIcon />}
            checkedIcon={<CheckCircleIcon />}
            checked={
              currentQuestion && currentQuestion.correctAnswer
                ? currentQuestion.correctAnswer === currentQuestion.firstAnswer
                : false
            }
            onChange={(e) =>
              handleCheckboxChange(e, currentQuestion?.firstAnswer)
            }
          />
        </Grid>
        <Grid className="answer" item xs={6}>
          <Typography className="letter" component="span">
            B
          </Typography>
          <TextField
            className="input"
            id="outlined-basic"
            label="Unesite odgovor"
            value={currentQuestion ? currentQuestion.secondAnswer : ""}
            onChange={(e) =>
              handleQuestionChange(selected, { secondAnswer: e.target.value })
            }
            multiline
          />
          <Checkbox
            className="question-checkbox"
            {...{ inputProps: { "aria-label": "Checkbox demo" } }}
            icon={<RadioButtonUncheckedIcon />}
            checkedIcon={<CheckCircleIcon />}
            checked={
              currentQuestion && currentQuestion.correctAnswer
                ? currentQuestion.correctAnswer === currentQuestion.secondAnswer
                : false
            }
            onChange={(e) =>
              handleCheckboxChange(e, currentQuestion?.secondAnswer)
            }
          />
        </Grid>
        <Grid className="answer" item xs={6}>
          <Typography className="letter" component="span">
            C
          </Typography>
          <TextField
            className="input"
            id="outlined-basic"
            label="Unesite odgovor"
            value={currentQuestion ? currentQuestion.thirdAnswer : ""}
            onChange={(e) =>
              handleQuestionChange(selected, { thirdAnswer: e.target.value })
            }
            multiline
          />
          <Checkbox
            className="question-checkbox"
            {...{ inputProps: { "aria-label": "Checkbox demo" } }}
            icon={<RadioButtonUncheckedIcon />}
            checkedIcon={<CheckCircleIcon />}
            checked={
              currentQuestion && currentQuestion.correctAnswer
                ? currentQuestion.correctAnswer === currentQuestion.thirdAnswer
                : false
            }
            onChange={(e) =>
              handleCheckboxChange(e, currentQuestion?.thirdAnswer)
            }
          />
        </Grid>
        <Grid className="answer" item xs={6}>
          <Typography className="letter" component="span">
            D
          </Typography>
          <TextField
            className="input"
            id="outlined-basic"
            label="Unesite odgovor"
            value={currentQuestion ? currentQuestion.fourthAnswer : ""}
            onChange={(e) =>
              handleQuestionChange(selected, { fourthAnswer: e.target.value })
            }
            multiline
          />
          <Checkbox
            className="question-checkbox"
            {...{ inputProps: { "aria-label": "Checkbox demo" } }}
            icon={<RadioButtonUncheckedIcon />}
            checkedIcon={<CheckCircleIcon />}
            checked={
              currentQuestion && currentQuestion.correctAnswer
                ? currentQuestion.correctAnswer === currentQuestion.fourthAnswer
                : false
            }
            onChange={(e) =>
              handleCheckboxChange(e, currentQuestion?.fourthAnswer)
            }
          />
        </Grid>
      </Grid>
    </Box>
  );
}
