import { Box } from "@mui/material";
import styled from "styled-components";

// Sign-in and registration share one screen: a painting with the app's name beside the form.
export const ScreenWrapper = styled(Box)`
  display: grid;
  grid-template-columns: minmax(0, 1fr) minmax(0, 1fr);
  min-height: 100vh;
  width: 100%;

  @media (max-width: 900px) {
    grid-template-columns: minmax(0, 1fr);
  }
`;

export const Art = styled(Box)`
  position: relative;
  display: flex;
  align-items: flex-end;
  overflow: hidden;
  background-color: #553b08;

  img.painting {
    position: absolute;
    inset: 0;
    width: 100%;
    height: 100%;
    object-fit: cover;
    object-position: center 30%;
  }

  /* A scrim under the words, so they read on any part of the painting. */
  &::after {
    content: "";
    position: absolute;
    inset: 0;
    background: linear-gradient(to top, rgba(42, 33, 21, 0.88) 0%, rgba(42, 33, 21, 0.35) 38%, rgba(42, 33, 21, 0) 62%);
  }

  @media (max-width: 900px) {
    display: none;
  }
`;

export const ArtText = styled(Box)`
  position: relative;
  z-index: 1;
  display: flex;
  align-items: center;
  gap: 1.25rem;
  padding: 3rem;
  color: #ffffff;

  img {
    width: 5rem;
    height: 5rem;
    flex-shrink: 0;
    border-radius: 50%;
    background-color: #e9e5cd;
  }

  h2 {
    margin: 0;
    font-family: "Literata", Georgia, serif;
    font-size: 2.25rem;
    font-weight: 600;
    line-height: 1.1;
  }

  p {
    margin: 0.4rem 0 0;
    max-width: 26rem;
    font-size: 1.1rem;
    line-height: 1.45;
    color: rgba(255, 255, 255, 0.9);
  }
`;

export const FormSide = styled(Box)`
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 2rem 1.25rem;
`;

export const FormWrapper = styled(Box)`
  display: flex;
  flex-direction: column;
  width: 100%;
  max-width: 24rem;

  form {
    display: flex;
    flex-direction: column;
    gap: 1rem;
  }
`;

export const FormTitleWrapper = styled(Box)`
  padding-bottom: 1.75rem;

  /* The app's mark stands above the form only where the painting is not shown. */
  img {
    display: none;
    width: 3.5rem;
    height: 3.5rem;
    margin-bottom: 1.25rem;
    border-radius: 50%;
    background-color: #e9e5cd;
  }

  p {
    margin-top: 0.4rem;
    color: #6a5d49;
  }

  @media (max-width: 900px) {
    img {
      display: block;
    }
  }
`;

export const FormFoot = styled(Box)`
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.25rem;
  margin-top: 1.25rem;
  padding-top: 1.25rem;
  border-top: 1px solid #e4dcc9;
  color: #6a5d49;
  font-size: 0.95rem;
`;
