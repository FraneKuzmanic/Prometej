import { Box, Paper } from "@mui/material";
import styled from "styled-components";

export const ScreenWrapper = styled(Box)`
  display: flex;
  height: 100vh;
  justify-content: center;
  align-items: center;
  width: 100%;
`;

export const FormWrapper = styled(Paper)`
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 2.5em;
`;

export const FormTitleWrapper = styled(Box)`
  padding-bottom: 2em;
`;
