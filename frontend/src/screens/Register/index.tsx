import { useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { useSelector } from "react-redux";
import { useForm } from "react-hook-form";
import { Box, Button, TextField, Typography, Alert } from "@mui/material";

import { RegisterInput, UserCreateRequest } from "../../types/models/User";
import { RootState, useAppDispatch } from "../../store/store";
import { clearRegistered, registerStudent } from "../../store/slices/userSlice";
import logo from "/logo.svg";
import AuthArt from "../Login/AuthArt";
import {
  FormFoot,
  FormSide,
  FormTitleWrapper,
  FormWrapper,
  ScreenWrapper,
} from "./index.styled";

const Register = () => {
  const dispatch = useAppDispatch();
  const navigate = useNavigate();
  const {
    register,
    watch,
    handleSubmit,
    formState: { errors },
  } = useForm<RegisterInput>();
  const { registered, registerError } = useSelector(
    (state: RootState) => state.user
  );

  useEffect(() => {
    if (registered) {
      navigate(`/login`);
      dispatch(clearRegistered());
    }
  }, [registered, dispatch, navigate]);

  const onSubmit = (data: RegisterInput) => {
    const user: UserCreateRequest = {
      firstName: data.firstName,
      lastName: data.lastName,
      email: data.email,
      password: data.password,
    };
    dispatch(registerStudent(user));
  };

  return (
    <ScreenWrapper>
      <AuthArt />
      <FormSide>
        <FormWrapper>
          <FormTitleWrapper>
            <img src={logo} alt="" />
            <Typography component="h1" variant="h4">
              Registrirajte se!
            </Typography>
            <Typography>
              Račun učenika: rezultati kvizova, napredak po razdobljima i rasprave.
            </Typography>
          </FormTitleWrapper>
          <Box component="form" width="100%" onSubmit={handleSubmit(onSubmit)}>
            {registerError ? (
              <Alert severity="error">
                {registerError === "conflict"
                  ? "Email adresa se već koristi."
                  : "Registracija nije uspjela. Provjerite podatke i pokušajte ponovo."}
              </Alert>
            ) : null}
            <Box sx={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 2 }}>
              <TextField
                {...register("firstName", {
                  required: "Ime je obavezno",
                })}
                label="Ime"
                name="firstName"
                id="firstName"
                autoComplete="given-name"
                fullWidth
                error={!!errors.firstName}
                helperText={errors.firstName?.message}
              />
              <TextField
                {...register("lastName", {
                  required: "Prezime je obavezno",
                })}
                label="Prezime"
                name="lastName"
                id="lastName"
                autoComplete="family-name"
                fullWidth
                error={!!errors.lastName}
                helperText={errors.lastName?.message}
              />
            </Box>
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
              error={!!errors.email}
              helperText={errors.email?.message}
            />
            <TextField
              {...register("password", {
                required: "Lozinka je obavezna",
                minLength: {
                  value: 8,
                  message: "Lozinka mora imati najmanje 8 znakova",
                },
                maxLength: {
                  value: 72,
                  message: "Lozinka može imati najviše 72 znaka",
                },
              })}
              label="Lozinka"
              name="password"
              type="password"
              id="password"
              autoComplete="new-password"
              fullWidth
              error={!!errors.password}
              helperText={errors.password?.message ?? "Najmanje 8 znakova"}
            />
            <TextField
              {...register("repeatedPassword", {
                validate: (val: string) => {
                  if (watch("password") != val) {
                    return "Lozinke se ne podudaraju";
                  }
                },
              })}
              label="Ponovite lozinku"
              name="repeatedPassword"
              type="password"
              id="repeatedPassword"
              autoComplete="new-password"
              fullWidth
              error={!!errors.repeatedPassword}
              helperText={errors.repeatedPassword?.message}
            />
            <Button type="submit" size="large" variant="contained" fullWidth>
              Registriraj se
            </Button>
          </Box>
          <FormFoot>
            <span>Već imate račun?</span>
            <Button type="button" variant="text" onClick={() => navigate(`/login`)}>
              Prijavi se
            </Button>
          </FormFoot>
        </FormWrapper>
      </FormSide>
    </ScreenWrapper>
  );
};

export default Register;
