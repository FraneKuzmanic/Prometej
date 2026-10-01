import { useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { useSelector } from "react-redux";
import { useForm } from "react-hook-form";
import {
  Box,
  Button,
  Container,
  TextField,
  Typography,
  Alert,
} from "@mui/material";

import { LoginInput } from "../../types/models/User";
import { RootState, useAppDispatch } from "../../store/store";
import { attemptLogin } from "../../store/slices/userSlice";
import { FormTitleWrapper, FormWrapper, ScreenWrapper } from "./index.styled";

const Login = () => {
  const dispatch = useAppDispatch();
  const navigate = useNavigate();
  const { user, authenticated } = useSelector((state: RootState) => state.user);
  const { register, handleSubmit } = useForm<LoginInput>();

  const onSubmit = (data: LoginInput) => {
    dispatch(attemptLogin(data));
  };

  useEffect(() => {
    if (user !== undefined) {
      localStorage.setItem("userId", user.id.toString());
      navigate("/learning");
    }
  }, [user]);

  return (
    <ScreenWrapper>
      <Container maxWidth="xs">
        <FormWrapper>
          <FormTitleWrapper>
            <Typography component="h1" variant="h5">
              Dobrodošli!
            </Typography>
          </FormTitleWrapper>
          <Box component="form" width="100%" onSubmit={handleSubmit(onSubmit)}>
            {authenticated === false ? (
              <Box marginBottom="20px">
                <Alert severity="error">
                  Neuspješna prijava. Provjerite podatke i pokušajte ponovo.
                </Alert>
              </Box>
            ) : null}
            <Box marginBottom="20px">
              <TextField
                {...register("email", {
                  required: "Email adresa je obavezna",
                })}
                label="Email adresa"
                name="email"
                type="email"
                id="email"
                fullWidth
              />
            </Box>
            <Box marginBottom="20px">
              <TextField
                {...register("password", {
                  required: "Lozinka je obavezna",
                })}
                label="Lozinka"
                name="password"
                id="password"
                type="password"
                fullWidth
              />
            </Box>
            <Button
              sx={{ marginBottom: "5px" }}
              type="submit"
              size="large"
              variant="contained"
              fullWidth
              style={{ backgroundColor: "#553b08" }}
            >
              Prijavi se
            </Button>
            <Button
              variant="text"
              fullWidth
              onClick={() => navigate("/register")}
              style={{ color: "#553b08" }}
            >
              Registriraj se
            </Button>
          </Box>
        </FormWrapper>
      </Container>
    </ScreenWrapper>
  );
};

export default Login;
