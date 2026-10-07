import { ReactNode } from "react";
import { Box, Typography } from "@mui/material";
import "./styles.css";

// The column every screen inside the shell is laid out in, so headings, tables and cards
// share their edges. "narrow" is for a form or a single list.
export function Page({ narrow, children }: { narrow?: boolean; children: ReactNode }) {
  return <Box className={narrow ? "page page-narrow" : "page"}>{children}</Box>;
}

interface PageHeaderProps {
  title: string;
  // one line under the title that says what the screen is for
  lead?: ReactNode;
  // controls that act on the whole screen, at the end of the title's row
  children?: ReactNode;
}

export function PageHeader({ title, lead, children }: PageHeaderProps) {
  return (
    <Box className="page-header">
      <Box className="page-header-text">
        <Typography variant="h4" component="h1">
          {title}
        </Typography>
        {lead && <Typography className="page-header-lead">{lead}</Typography>}
      </Box>
      {children && <Box className="page-header-actions">{children}</Box>}
    </Box>
  );
}

interface EmptyStateProps {
  icon: ReactNode;
  title: string;
  // what to do next, in a sentence or two
  children?: ReactNode;
  action?: ReactNode;
}

export function EmptyState({ icon, title, children, action }: EmptyStateProps) {
  return (
    <Box className="empty-state">
      <Box className="empty-state-icon" aria-hidden="true">
        {icon}
      </Box>
      <Typography variant="h5" component="h2">
        {title}
      </Typography>
      {children && <Typography className="empty-state-text">{children}</Typography>}
      {action && <Box className="empty-state-action">{action}</Box>}
    </Box>
  );
}
