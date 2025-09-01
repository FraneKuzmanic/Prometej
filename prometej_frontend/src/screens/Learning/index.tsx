import { styled } from "@mui/material/styles";
import Grid from "@mui/material/Unstable_Grid2";
import Paper from "@mui/material/Paper";
import Box from "@mui/material/Box";
import { Button, Typography } from "@mui/material";
import "./styles.css";
import ArrowForwardIcon from "@mui/icons-material/ArrowForward";
import { useNavigate } from "react-router-dom";

const Item = styled(Paper)(({ theme }) => ({
  backgroundColor: theme.palette.mode === "dark" ? "#1A2027" : "#fff",
  ...theme.typography.body2,
  padding: theme.spacing(0),
  color: theme.palette.text.secondary,
}));

export default function Learning() {
  const navigate = useNavigate();

  const openLiteraryPeriod = (id: number) => {
    navigate(`/learning/${id}`);
  };

  return (
    <Box sx={{ width: "100%" }}>
      <Grid container rowSpacing={10} columnSpacing={{ xs: 1, sm: 2, md: 3 }}>
        <Grid xs={6}>
          <Item className="item">
            <Box sx={{ padding: 2 }}>
              <Typography variant="h4">Antika</Typography>
              <Typography variant="body1">
                Zaplovite u svijet davnina književnosti, od drevnih civilizacija
                antičke Grčke, drevnog Rima te najpoznatijih tragedija, komedija
                te epova
              </Typography>
              <Button
                onClick={() => openLiteraryPeriod(1)}
                className="button"
                variant="text"
              >
                Idi na <ArrowForwardIcon style={{ marginLeft: 4 }} />
              </Button>
            </Box>
            <Box className="img-container">
              <img className="img" src="/antika.png" alt="antika" />
            </Box>
          </Item>
        </Grid>
        <Grid xs={6}>
          <Item className="item">
            <Box sx={{ padding: 2 }}>
              <Typography variant="h4">Srednji vijek</Typography>
              <Typography variant="body1">
                Naučite više o mračnom i najduljem razdoblju književnosti,
                viteškim romanima, utjecaju crkve na književnost te
                najpoznatijim piscima tog doba
              </Typography>
              <Button
                onClick={() => openLiteraryPeriod(2)}
                className="button"
                variant="text"
              >
                Idi na <ArrowForwardIcon style={{ marginLeft: 4 }} />
              </Button>
            </Box>
            <Box className="img-container">
              <img
                className="img"
                src="/srednji_vijek.png"
                alt="srednji vijek"
              />
            </Box>
          </Item>
        </Grid>
        <Grid xs={6}>
          <Item className="item">
            <Box sx={{ padding: 2 }}>
              <Typography variant="h4">Renesansa</Typography>
              <Typography variant="body1">
                Otkrijte svijet renesansne književnosti, motiv čovjeka u
                središtu svemira, humanizam, te začeće romana kao književnog
                žanra
              </Typography>
              <Button
                onClick={() => openLiteraryPeriod(3)}
                className="button"
                variant="text"
              >
                Idi na <ArrowForwardIcon style={{ marginLeft: 4 }} />
              </Button>
            </Box>
            <Box className="img-container">
              <img className="img" src="/renesansa.png" alt="renesansa" />
            </Box>
          </Item>
        </Grid>
        <Grid xs={6}>
          <Item className="item">
            <Box sx={{ padding: 2 }}>
              <Typography variant="h4">Realizam</Typography>
              <Typography variant="body1">
                Realizam obilježava realan opis svijeta. Fokus je na dugačkom i
                detaljnom opisu likova i prostora što donosi brojne
                zanimljivosti iz tog vremena.
              </Typography>
              <Button
                onClick={() => openLiteraryPeriod(4)}
                className="button"
                variant="text"
              >
                Idi na <ArrowForwardIcon style={{ marginLeft: 4 }} />
              </Button>
            </Box>
            <Box className="img-container">
              <img className="img" src="/realizam.png" alt="realizam" />
            </Box>
          </Item>
        </Grid>
        <Grid xs={6}>
          <Item className="item">
            <Box sx={{ padding: 2 }}>
              <Typography variant="h4">Romantizam</Typography>
              <Typography variant="body1">
                Osjetite romantizam, razdoblje koje obilježava osjećajnost,
                maštu, prirodu, fokus na pojedinca, njegovu impresiju i
                ekspresiju.
              </Typography>
              <Button
                onClick={() => openLiteraryPeriod(5)}
                className="button"
                variant="text"
              >
                Idi na <ArrowForwardIcon style={{ marginLeft: 4 }} />
              </Button>
            </Box>
            <Box className="img-container">
              <img className="img" src="/romantizam.png" alt="Romantizam" />
            </Box>
          </Item>
        </Grid>
        <Grid xs={6}>
          <Item className="item">
            <Box sx={{ padding: 2 }}>
              <Typography variant="h4">Modernizam</Typography>
              <Typography variant="body1">
                Modernizam obilježava razdoblje estetike u književnosti,
                umjetnost radi same sebe. Zavirite u čari modernizma, njegove
                teme, motive i najpoznatije autore.
              </Typography>
              <Button
                onClick={() => openLiteraryPeriod(6)}
                className="button"
                variant="text"
              >
                Idi na <ArrowForwardIcon style={{ marginLeft: 4 }} />
              </Button>
            </Box>
            <Box className="img-container">
              <img className="img" src="/modernizam.png" alt="modernizam" />
            </Box>
          </Item>
        </Grid>
      </Grid>
    </Box>
  );
}
