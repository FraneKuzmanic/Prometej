import { PropsWithChildren } from "react";
import { useSelector } from "react-redux";
import { Navigate } from "react-router-dom";
import { Box, CircularProgress } from "@mui/material";
import { RootState } from "../../store/store";
import ROLE from "../../types/enums/Role";

type RequireRoleProps = PropsWithChildren<{ roles: ROLE[] }>;

// Keeps a screen away from users who cannot use it. This is a convenience for the
// user, not the protection itself: the server checks the role on every request.
const RequireRole = ({ roles, children }: RequireRoleProps) => {
  const { user, authenticated } = useSelector((state: RootState) => state.user);

  // The session check has not answered yet; redirecting now would log out every reload.
  if (authenticated === undefined) {
    return (
      <Box sx={{ display: "flex", justifyContent: "center", marginTop: 8 }}>
        <CircularProgress />
      </Box>
    );
  }

  if (!authenticated || !user) {
    return <Navigate to="/login" replace />;
  }

  if (!roles.includes(user.role)) {
    return <Navigate to="/learning" replace />;
  }

  return children;
};

export default RequireRole;
