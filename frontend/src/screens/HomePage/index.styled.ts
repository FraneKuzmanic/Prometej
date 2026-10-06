import { styled, Theme, CSSObject } from '@mui/material/styles';
import MuiDrawer from '@mui/material/Drawer';
import MuiAppBar, { AppBarProps as MuiAppBarProps } from '@mui/material/AppBar';
import { alpha } from '@mui/material/styles';
import InputBase from '@mui/material/InputBase';
import { Box } from '@mui/material';

export const ScreenWrapper = styled(Box)`
  display: flex;
  height: 100vh;
`;

// The icon and the clear button are adornments of the one input, so a click anywhere on the
// field lands in it.
export const SearchField = styled(InputBase)(({ theme }) => ({
  width: '100%',
  maxWidth: 480,
  height: 40,
  padding: theme.spacing(0, 0.5, 0, 1.5),
  gap: theme.spacing(1),
  borderRadius: theme.shape.borderRadius,
  color: 'inherit',
  cursor: 'text',
  backgroundColor: alpha(theme.palette.common.white, 0.14),
  transition: 'background-color 140ms ease-out, box-shadow 140ms ease-out',
  '&:hover': {
    backgroundColor: alpha(theme.palette.common.white, 0.2),
  },
  // The page's brown focus ring would not show on the brown header.
  '&.Mui-focused': {
    backgroundColor: alpha(theme.palette.common.white, 0.24),
    boxShadow: `0 0 0 2px ${alpha(theme.palette.common.white, 0.6)}`,
  },
  '& .MuiInputBase-input': {
    padding: 0,
    height: '100%',
    outline: 'none',
    '&::placeholder': {
      color: alpha(theme.palette.common.white, 0.82),
      opacity: 1,
    },
  },
}));


export const drawerWidth = 240;

export const openedMixin = (theme: Theme): CSSObject => ({
  width: drawerWidth,
  transition: theme.transitions.create('width', {
    easing: theme.transitions.easing.sharp,
    duration: theme.transitions.duration.enteringScreen,
  }),
  overflowX: 'hidden',
});

export const closedMixin = (theme: Theme): CSSObject => ({
  transition: theme.transitions.create('width', {
    easing: theme.transitions.easing.sharp,
    duration: theme.transitions.duration.leavingScreen,
  }),
  overflowX: 'hidden',
  width: `calc(${theme.spacing(7)} + 1px)`,
  [theme.breakpoints.up('sm')]: {
    width: `calc(${theme.spacing(8)} + 1px)`,
  },
});

export const DrawerHeader = styled('div')(({ theme }) => ({
  display: 'flex',
  alignItems: 'center',
  justifyContent: 'flex-end',
  padding: theme.spacing(0, 1),
  // necessary for content to be below app bar
  ...theme.mixins.toolbar,
}));

interface AppBarProps extends MuiAppBarProps {
  open?: boolean;
}

export const AppBar = styled(MuiAppBar, {
  shouldForwardProp: (prop) => prop !== 'open',
})<AppBarProps>(({ theme, open }) => ({
  transition: theme.transitions.create(['width', 'margin'], {
    easing: theme.transitions.easing.sharp,
    duration: theme.transitions.duration.leavingScreen,
  }),
  backgroundColor: theme.palette.primary.main,
  boxShadow: '0 1px 2px rgba(42, 33, 21, 0.18), 0 4px 14px rgba(60, 40, 10, 0.12)',
  // Beside the drawer in both of its widths, never under it.
  ...(!open && {
    width: `calc(100% - ${theme.spacing(7)} - 1px)`,
    [theme.breakpoints.up('sm')]: {
      width: `calc(100% - ${theme.spacing(8)} - 1px)`,
    },
  }),
  ...(open && {
    width: `calc(100% - ${drawerWidth}px)`,
    transition: theme.transitions.create(['width', 'margin'], {
      easing: theme.transitions.easing.sharp,
      duration: theme.transitions.duration.enteringScreen,
    }),
  }),
}));

export const Drawer = styled(MuiDrawer, { shouldForwardProp: (prop) => prop !== 'open' })(
  ({ theme, open }) => ({
    width: drawerWidth,
    flexShrink: 0,
    whiteSpace: 'nowrap',
    boxSizing: 'border-box',
    ...(open && {
      ...openedMixin(theme),
      '& .MuiDrawer-paper': openedMixin(theme),
    }),
    ...(!open && {
      ...closedMixin(theme),
      '& .MuiDrawer-paper': closedMixin(theme),
    }),
  }),
);