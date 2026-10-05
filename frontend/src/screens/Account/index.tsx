import { useState } from "react";
import { useSelector } from "react-redux";
import { useNavigate } from "react-router-dom";
import { useForm } from "react-hook-form";
import {
  Alert,
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
  Paper,
  TextField,
  Typography,
} from "@mui/material";
import { RootState, useAppDispatch } from "../../store/store";
import {
  changePassword,
  deleteCurrentUser,
  updateName,
} from "../../store/slices/userSlice";
import {
  PasswordInput,
  UserNameEditRequest,
} from "../../types/models/User";
import { roleLabels } from "../../types/enums/Role";
import "./styles.css";

export default function Account() {
  const { user } = useSelector((state: RootState) => state.user);
  const dispatch = useAppDispatch();
  const navigate = useNavigate();
  const nameForm = useForm<UserNameEditRequest>({
    defaultValues: { firstName: user?.firstName, lastName: user?.lastName },
  });
  const passwordForm = useForm<PasswordInput>();
  const [nameResult, setNameResult] = useState<"ok" | "failed" | undefined>();
  const [passwordResult, setPasswordResult] = useState<
    "ok" | "wrongPassword" | "failed" | undefined
  >();
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  // "conflict": the server refuses to delete an account that still has Quizzes.
  const [deleteError, setDeleteError] = useState<
    "conflict" | "other" | undefined
  >();

  const onNameSubmit = (data: UserNameEditRequest) => {
    setNameResult(undefined);
    const name = {
      firstName: data.firstName.trim(),
      lastName: data.lastName.trim(),
    };
    dispatch(updateName(name)).then((result) => {
      setNameResult(updateName.fulfilled.match(result) ? "ok" : "failed");
    });
  };

  const onPasswordSubmit = (data: PasswordInput) => {
    setPasswordResult(undefined);
    const passwords = {
      currentPassword: data.currentPassword,
      newPassword: data.newPassword,
    };
    dispatch(changePassword(passwords)).then((result) => {
      if (changePassword.fulfilled.match(result)) {
        passwordForm.reset();
        setPasswordResult("ok");
      } else {
        // The server answers a wrong current password with 400, not 401.
        setPasswordResult(result.payload === 400 ? "wrongPassword" : "failed");
      }
    });
  };

  const handleDeleteAccount = (): void => {
    dispatch(deleteCurrentUser()).then((result) => {
      if (deleteCurrentUser.fulfilled.match(result)) {
        setDeleteDialogOpen(false);
        navigate("/learning");
      } else {
        setDeleteError(result.payload === 409 ? "conflict" : "other");
      }
    });
  };

  const nameErrors = nameForm.formState.errors;
  const passwordErrors = passwordForm.formState.errors;

  return (
    <Box className="account-wrapper">
      <Typography variant="h3" className="account-title">
        Moj račun
      </Typography>
      {user && (
        <Typography>
          {user.email} · {roleLabels[user.role]}
        </Typography>
      )}
      <Paper className="account-section">
        <Typography variant="h5" className="account-heading">
          Ime i prezime
        </Typography>
        <Box
          component="form"
          className="account-form"
          noValidate
          onSubmit={nameForm.handleSubmit(onNameSubmit, () =>
            setNameResult(undefined)
          )}
        >
          <TextField
            {...nameForm.register("firstName", {
              validate: (value) => value.trim() !== "" || "Ime je obavezno",
              maxLength: {
                value: 100,
                message: "Ime može imati najviše 100 znakova",
              },
            })}
            label="Ime"
            id="firstName"
            fullWidth
            error={!!nameErrors.firstName}
            helperText={nameErrors.firstName?.message}
          />
          <TextField
            {...nameForm.register("lastName", {
              validate: (value) => value.trim() !== "" || "Prezime je obavezno",
              maxLength: {
                value: 100,
                message: "Prezime može imati najviše 100 znakova",
              },
            })}
            label="Prezime"
            id="lastName"
            fullWidth
            error={!!nameErrors.lastName}
            helperText={nameErrors.lastName?.message}
          />
          {nameResult === "ok" && (
            <Alert severity="success">Ime je promijenjeno.</Alert>
          )}
          {nameResult === "failed" && (
            <Alert severity="error">
              Ime nije promijenjeno. Pokušajte ponovno.
            </Alert>
          )}
          <Button
            type="submit"
            variant="contained"
            sx={{ backgroundColor: "#553b08", alignSelf: "flex-start" }}
          >
            Spremi
          </Button>
        </Box>
      </Paper>
      <Paper className="account-section">
        <Typography variant="h5" className="account-heading">
          Lozinka
        </Typography>
        <Box
          component="form"
          className="account-form"
          noValidate
          onSubmit={passwordForm.handleSubmit(onPasswordSubmit, () =>
            setPasswordResult(undefined)
          )}
        >
          <TextField
            {...passwordForm.register("currentPassword", {
              required: "Trenutna lozinka je obavezna",
            })}
            label="Trenutna lozinka"
            id="currentPassword"
            type="password"
            autoComplete="current-password"
            fullWidth
            error={!!passwordErrors.currentPassword}
            helperText={passwordErrors.currentPassword?.message}
          />
          <TextField
            {...passwordForm.register("newPassword", {
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
            label="Nova lozinka"
            id="newPassword"
            type="password"
            autoComplete="new-password"
            fullWidth
            error={!!passwordErrors.newPassword}
            helperText={passwordErrors.newPassword?.message}
          />
          <TextField
            {...passwordForm.register("repeatedPassword", {
              validate: (value) =>
                passwordForm.watch("newPassword") === value ||
                "Lozinke se ne preklapaju",
            })}
            label="Ponovite novu lozinku"
            id="repeatedPassword"
            type="password"
            autoComplete="new-password"
            fullWidth
            error={!!passwordErrors.repeatedPassword}
            helperText={passwordErrors.repeatedPassword?.message}
          />
          {passwordResult === "ok" && (
            <Alert severity="success">
              Lozinka je promijenjena. Na drugim uređajima ste odjavljeni.
            </Alert>
          )}
          {passwordResult === "wrongPassword" && (
            <Alert severity="error">Trenutna lozinka nije točna.</Alert>
          )}
          {passwordResult === "failed" && (
            <Alert severity="error">
              Lozinka nije promijenjena. Pokušajte ponovno.
            </Alert>
          )}
          <Button
            type="submit"
            variant="contained"
            sx={{ backgroundColor: "#553b08", alignSelf: "flex-start" }}
          >
            Promijeni lozinku
          </Button>
        </Box>
      </Paper>
      <Paper className="account-section">
        <Typography variant="h5" className="account-heading">
          Brisanje računa
        </Typography>
        <Typography sx={{ marginBottom: 2 }}>
          Brisanje računa je trajno: s računom se brišu i vaši rezultati.
        </Typography>
        <Button
          color="error"
          variant="outlined"
          onClick={() => {
            setDeleteError(undefined);
            setDeleteDialogOpen(true);
          }}
        >
          Obriši račun
        </Button>
      </Paper>
      <Dialog
        open={deleteDialogOpen}
        onClose={() => setDeleteDialogOpen(false)}
      >
        <DialogTitle>Obriši račun</DialogTitle>
        <DialogContent>
          <DialogContentText>
            Jeste li sigurni? Vaš račun i vaši rezultati bit će trajno
            obrisani.
          </DialogContentText>
          {deleteError && (
            <DialogContentText color="error" sx={{ marginTop: 2 }}>
              {deleteError === "conflict"
                ? "Račun se ne može obrisati dok imate kvizove. Prvo obrišite svoje kvizove u „Moji kvizovi”."
                : "Račun nije obrisan. Pokušajte ponovno."}
            </DialogContentText>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDeleteDialogOpen(false)}>Odustani</Button>
          <Button color="error" onClick={handleDeleteAccount}>
            Obriši
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  );
}
