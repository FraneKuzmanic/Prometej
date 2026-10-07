import { useEffect, useMemo, useState } from "react";
import { useSelector } from "react-redux";
import {
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
  InputAdornment,
  MenuItem,
  Paper,
  Select,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from "@mui/material";
import { RootState, useAppDispatch } from "../../store/store";
import { fetchUsers, setUserRole } from "../../store/slices/userSlice";
import { UserAccount } from "../../types/models/User";
import ROLE, { roleLabels } from "../../types/enums/Role";
import SearchIcon from "@mui/icons-material/Search";
import { Page, PageHeader } from "../../components/Page";
import Initials from "../../components/Initials";
import "./styles.css";

const collator = new Intl.Collator("hr");

export default function Users() {
  const { user, users, usersFailed } = useSelector(
    (state: RootState) => state.user
  );
  const dispatch = useAppDispatch();
  const [query, setQuery] = useState("");
  // Kept after the dialog closes, so its text does not change while it fades out.
  const [pending, setPending] = useState<{
    account: UserAccount;
    role: ROLE;
  } | null>(null);
  const [dialogOpen, setDialogOpen] = useState(false);
  // "conflict": a User with Quizzes cannot be made a Student.
  const [roleError, setRoleError] = useState<
    "conflict" | "other" | undefined
  >();

  useEffect(() => {
    const request = dispatch(fetchUsers());
    return () => request.abort();
  }, [dispatch]);

  const rows = useMemo(() => {
    const wanted = query.trim().toLocaleLowerCase("hr");
    return (users ?? [])
      .filter((account) =>
        `${account.firstName} ${account.lastName} ${account.email}`
          .toLocaleLowerCase("hr")
          .includes(wanted)
      )
      .sort(
        (a, b) =>
          collator.compare(a.lastName, b.lastName) ||
          collator.compare(a.firstName, b.firstName)
      );
  }, [users, query]);

  const handleConfirm = () => {
    if (!pending) return;
    dispatch(
      setUserRole({ id: pending.account.id, role: pending.role })
    ).then((result) => {
      if (setUserRole.fulfilled.match(result)) {
        setDialogOpen(false);
      } else {
        setRoleError(result.payload === 409 ? "conflict" : "other");
      }
    });
  };

  return (
    <Page>
      <PageHeader
        title="Korisnici"
        lead="Svi računi i njihove uloge. Registracijom nastaje učenik; nastavnika i administratora postavljate ovdje."
      >
        {users && (
          <TextField
            className="users-filter"
            size="small"
            label="Pretraži korisnike"
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            InputProps={{
              startAdornment: (
                <InputAdornment position="start">
                  <SearchIcon fontSize="small" />
                </InputAdornment>
              ),
            }}
          />
        )}
      </PageHeader>
      {usersFailed && (
        <Typography>Korisnici se nisu učitali. Pokušajte ponovno.</Typography>
      )}
      {users && (
        <>
          {rows.length === 0 ? (
            <Typography color="text.secondary">Nema korisnika za prikaz.</Typography>
          ) : (
            <TableContainer component={Paper}>
              <Table aria-label="Korisnici">
                <TableHead>
                  <TableRow>
                    <TableCell>Ime i prezime</TableCell>
                    <TableCell>Email adresa</TableCell>
                    <TableCell align="right">Kvizovi</TableCell>
                    <TableCell>Uloga</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {rows.map((account) => (
                    <TableRow key={account.id} hover>
                      <TableCell component="th" scope="row">
                        {/* The flex box is inside the cell: a cell that is itself flex
                            loses its row's bottom border. */}
                        <Box className="users-name">
                          <Initials
                            name={`${account.firstName} ${account.lastName}`}
                            small
                          />
                          <span>
                            {account.firstName} {account.lastName}
                          </span>
                        </Box>
                      </TableCell>
                      <TableCell>{account.email}</TableCell>
                      <TableCell align="right">{account.quizCount}</TableCell>
                      <TableCell>
                        {account.id === user?.id ? (
                          roleLabels[account.role]
                        ) : (
                          <Select
                            size="small"
                            className="users-role"
                            value={account.role}
                            inputProps={{
                              "aria-label": `Uloga: ${account.firstName} ${account.lastName}`,
                            }}
                            onChange={(e) => {
                              setPending({
                                account,
                                role: e.target.value as ROLE,
                              });
                              setRoleError(undefined);
                              setDialogOpen(true);
                            }}
                          >
                            {Object.values(ROLE).map((role) => (
                              <MenuItem key={role} value={role}>
                                {roleLabels[role]}
                              </MenuItem>
                            ))}
                          </Select>
                        )}
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </TableContainer>
          )}
        </>
      )}
      <Dialog open={dialogOpen} onClose={() => setDialogOpen(false)}>
        <DialogTitle>Promjena uloge</DialogTitle>
        <DialogContent>
          <DialogContentText>
            Nova uloga korisnika {pending?.account.firstName}{" "}
            {pending?.account.lastName}: {pending && roleLabels[pending.role]}.
          </DialogContentText>
          {roleError && (
            <DialogContentText color="error" sx={{ marginTop: 2 }}>
              {roleError === "conflict"
                ? "Korisnik ima kvizove. Dok se ne obrišu, ne može postati učenik."
                : "Uloga nije promijenjena. Pokušajte ponovno."}
            </DialogContentText>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDialogOpen(false)}>Odustani</Button>
          <Button variant="contained" onClick={handleConfirm}>
            Potvrdi
          </Button>
        </DialogActions>
      </Dialog>
    </Page>
  );
}
