import { alpha, createTheme } from "@mui/material";

// The same two families are named in index.css for the hand-written stylesheets.
const sans = '"Source Sans 3", "Segoe UI", system-ui, sans-serif';
const serif = '"Literata", Georgia, "Times New Roman", serif';

const brown = "#553b08";
const ink = "#2a2115";
const inkMuted = "#6a5d49";
const paper = "#ffffff";
const page = "#f7f3e9";
const rule = "#e4dcc9";

const heading = { fontFamily: serif, fontWeight: 600, color: ink };

// The brown of the header and the sidebar is the app's primary colour, so links, focused
// fields and buttons no longer fall back to MUI's blue.
export const theme = createTheme({
  palette: {
    primary: { main: brown },
    background: { default: page, paper },
    text: { primary: ink, secondary: inkMuted },
    divider: rule,
    action: {
      hover: alpha(brown, 0.06),
      selected: alpha(brown, 0.1),
      focus: alpha(brown, 0.14),
    },
  },
  shape: { borderRadius: 8 },
  typography: {
    fontFamily: sans,
    h1: heading,
    h2: heading,
    h3: heading,
    h4: heading,
    h5: heading,
    h6: heading,
    button: { textTransform: "none", fontWeight: 600 },
  },
  components: {
    MuiButton: {
      defaultProps: { disableElevation: true },
      styleOverrides: {
        root: {
          transition:
            "background-color 160ms ease-out, border-color 160ms ease-out, transform 120ms ease-out",
          "&:active": { transform: "translateY(1px)" },
        },
      },
    },
    MuiPaper: {
      styleOverrides: {
        elevation1: {
          border: `1px solid ${rule}`,
          boxShadow: "0 1px 2px rgba(60, 40, 10, 0.05), 0 4px 12px rgba(60, 40, 10, 0.05)",
        },
      },
    },
    MuiMenu: {
      styleOverrides: {
        paper: {
          marginTop: 6,
          border: `1px solid ${rule}`,
          borderRadius: 10,
          boxShadow: "0 2px 4px rgba(60, 40, 10, 0.06), 0 12px 28px rgba(60, 40, 10, 0.14)",
        },
        list: { padding: 6 },
      },
    },
    MuiMenuItem: {
      styleOverrides: {
        root: {
          minHeight: 40,
          borderRadius: 6,
          transition: "background-color 120ms ease-out",
        },
      },
    },
    MuiDialog: {
      styleOverrides: {
        paper: {
          borderRadius: 14,
          boxShadow: "0 4px 8px rgba(60, 40, 10, 0.08), 0 24px 56px rgba(60, 40, 10, 0.22)",
        },
      },
    },
    MuiOutlinedInput: {
      styleOverrides: {
        root: { backgroundColor: paper },
        notchedOutline: { borderColor: rule, transition: "border-color 120ms ease-out" },
      },
    },
    MuiTooltip: {
      styleOverrides: {
        tooltip: { backgroundColor: ink, fontSize: "0.8rem" },
      },
    },
  },
});

export default theme;
