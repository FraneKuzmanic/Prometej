import { createTheme } from "@mui/material";

// The brown of the header and the sidebar is the app's primary colour, so links, focused
// fields and buttons no longer fall back to MUI's blue.
export const theme = createTheme({
  palette: {
    primary: { main: "#553b08" },
  },
  typography: {
    button: { textTransform: "none" },
  },
});

export default theme;
