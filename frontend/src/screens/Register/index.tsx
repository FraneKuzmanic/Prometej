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
  Stack,
} from "@mui/material";

import { RegisterInput, UserCreateRequest } from "../../types/models/User";
import { RootState, useAppDispatch } from "../../store/store";
import { clearRegistered, registerStudent } from "../../store/slices/userSlice";
import { FormTitleWrapper, FormWrapper, ScreenWrapper } from "./index.styled";
import ROLE from "../../types/enums/Role";

const Register = () => {
  const dispatch = useAppDispatch();
  const navigate = useNavigate();
  const {
    register,
    watch,
    handleSubmit,
    formState: { errors },
  } = useForm<RegisterInput>();
  const { registered } = useSelector((state: RootState) => state.user);

  useEffect(() => {
    if (registered) {
      navigate(`/login`);
      dispatch(clearRegistered());
    }
  }, [registered]);

  const onSubmit = (data: RegisterInput) => {
    const user: UserCreateRequest = {
      firstName: data.firstName,
      lastName: data.lastName,
      email: data.email,
      password: data.password,
      role: ROLE.Student,
    };
    dispatch(registerStudent(user));
  };

  return (
    <ScreenWrapper>
      <Container maxWidth="xs">
        <FormWrapper>
          <FormTitleWrapper>
            <Typography component="h1" variant="h5">
              Registrirajte se!
            </Typography>
          </FormTitleWrapper>
          <Box component="form" width="100%" onSubmit={handleSubmit(onSubmit)}>
            {registered === false ? (
              <Box marginBottom="20px">
                <Stack spacing={1}>
                  <Alert severity="error">
                    Registracija nije uspjela. Provjerite podatke i pokušajte
                    ponovo.
                  </Alert>
                </Stack>
              </Box>
            ) : null}
            <Box marginBottom="20px">
              <TextField
                {...register("firstName", {
                  required: "Ime je obavezno",
                })}
                label="Ime"
                name="firstName"
                id="firstName"
                fullWidth
              />
              {errors.firstName && (
                <div style={{ color: "red" }}>{errors.firstName.message}</div>
              )}
            </Box>
            <Box marginBottom="20px">
              <TextField
                {...register("lastName", {
                  required: "Prezime je obavezno",
                })}
                label="Prezime"
                name="lastName"
                id="lastName"
                fullWidth
              />
              {errors.lastName && (
                <div style={{ color: "red" }}>{errors.lastName.message}</div>
              )}
            </Box>
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
              {errors.email && (
                <div style={{ color: "red" }}>{errors.email.message}</div>
              )}
            </Box>
            <Box marginBottom="20px">
              <TextField
                {...register("password", {
                  required: "Lozinka je obavezna",
                })}
                label="Lozinka"
                name="password"
                type="password"
                id="password"
                fullWidth
              />
              {errors.password && (
                <div style={{ color: "red" }}>{errors.password.message}</div>
              )}
            </Box>
            <Box marginBottom="20px">
              <TextField
                {...register("repeatedPassword", {
                  required: "",
                  validate: (val: string) => {
                    if (watch("password") != val) {
                      return "Lozinke se ne preklapaju";
                    }
                  },
                })}
                label="Ponovite lozinku"
                name="repeatedPassword"
                type="password"
                id="repeatedPassword"
                fullWidth
              />
              {errors.repeatedPassword && (
                <div style={{ color: "red" }}>
                  {errors.repeatedPassword.message}
                </div>
              )}
            </Box>
            <Button
              sx={{ marginBottom: "5px" }}
              type="submit"
              size="large"
              variant="contained"
              fullWidth
              style={{ backgroundColor: "#553b08" }}
            >
              Registriraj se
            </Button>
            <Button
              type="button"
              variant="text"
              fullWidth
              onClick={() => navigate(`/login`)}
              style={{ color: "#553b08" }}
            >
              Prijavi se
            </Button>
          </Box>
        </FormWrapper>
      </Container>
    </ScreenWrapper>
  );
};

export default Register;
