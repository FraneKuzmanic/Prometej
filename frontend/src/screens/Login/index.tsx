import { useEffect } from "react";
import { Link as RouterLink, useNavigate } from "react-router-dom";
import { useSelector } from "react-redux";
import { useForm } from "react-hook-form";
import { Box, Button, Link, TextField, Typography, Alert } from "@mui/material";

import { LoginInput } from "../../types/models/User";
import { RootState, useAppDispatch } from "../../store/store";
import { attemptLogin } from "../../store/slices/userSlice";
import logo from "/logo.svg";
import AuthArt from "./AuthArt";
import {
  FormFoot,
  FormSide,
  FormTitleWrapper,
  FormWrapper,
  ScreenWrapper,
} from "./index.styled";

const Login = () => {
  const dispatch = useAppDispatch();
  const navigate = useNavigate();
  const { user, loginFailed } = useSelector((state: RootState) => state.user);
  const { register, handleSubmit } = useForm<LoginInput>();

  const onSubmit = (data: LoginInput) => {
    dispatch(attemptLogin(data));
  };

  useEffect(() => {
    if (user !== undefined) {
      navigate("/learning");
    }
  }, [user, navigate]);

  return (
    <ScreenWrapper>
      <AuthArt />
      <FormSide>
        <FormWrapper>
          <FormTitleWrapper>
            <img src={logo} alt="" />
            <Typography component="h1" variant="h4">
              Dobrodošli!
            </Typography>
            <Typography>
              Prijavite se da bi se vaši rezultati spremali.
            </Typography>
          </FormTitleWrapper>
          <Box component="form" width="100%" onSubmit={handleSubmit(onSubmit)}>
            {loginFailed ? (
              <Alert severity="error">
                Neuspješna prijava. Provjerite podatke i pokušajte ponovo.
              </Alert>
            ) : null}
            <TextField
              {...register("email", {
                required: "Email adresa je obavezna",
              })}
              label="Email adresa"
              name="email"
              type="email"
              id="email"
              autoComplete="email"
              fullWidth
            />
            <TextField
              {...register("password", {
                required: "Lozinka je obavezna",
              })}
              label="Lozinka"
              name="password"
              id="password"
              type="password"
              autoComplete="current-password"
              fullWidth
            />
            <Button type="submit" size="large" variant="contained" fullWidth>
              Prijavi se
            </Button>
          </Box>
          <FormFoot>
            <span>Nemate račun?</span>
            <Button variant="text" onClick={() => navigate("/register")}>
              Registriraj se
            </Button>
            <Link component={RouterLink} to="/learning" underline="hover">
              Nastavi bez prijave
            </Link>
          </FormFoot>
        </FormWrapper>
      </FormSide>
    </ScreenWrapper>
  );
};

export default Login;
