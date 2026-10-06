const twoDigits = (value: number) => String(value).padStart(2, "0");

// A stored time as a datetime-local field shows it: in the browser's own time, without a zone.
export const toLocalInput = (iso: string) => {
  const date = new Date(iso);
  return (
    `${date.getFullYear()}-${twoDigits(date.getMonth() + 1)}-${twoDigits(date.getDate())}` +
    `T${twoDigits(date.getHours())}:${twoDigits(date.getMinutes())}`
  );
};

// What was typed is the Teacher's local time; the server gets it with its zone.
export const fromLocalInput = (value: string) => new Date(value).toISOString();
