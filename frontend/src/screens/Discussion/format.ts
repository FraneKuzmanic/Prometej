export const formatDateTime = (date: string) =>
  new Date(date).toLocaleString("hr-HR", {
    year: "numeric",
    month: "numeric",
    day: "numeric",
    hour: "numeric",
    minute: "2-digit",
  });

// The Croatian word after a number of Replies: 1 odgovor, 3 odgovora, 11 odgovora, 21 odgovor.
export const repliesLabel = (count: number) =>
  count % 10 === 1 && count % 100 !== 11 ? "odgovor" : "odgovora";
