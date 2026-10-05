import { Typography } from "@mui/material";

// "limit": the server lets one User post only so often.
export type PostErrorKind = "limit" | "other";

// Why a Topic or a Reply was not stored.
export default function PostError({ error }: { error: PostErrorKind }) {
  return (
    <Typography color="error" role="alert">
      {error === "limit"
        ? "Pričekajte trenutak prije sljedeće objave."
        : "Objava nije spremljena. Pokušajte ponovno."}
    </Typography>
  );
}
