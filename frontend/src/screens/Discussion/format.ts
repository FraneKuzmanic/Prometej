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

// After a number of Topics: 1 tema, 2 teme, 5 tema, 12 tema, 22 teme.
export const topicsLabel = (count: number) => {
  const ones = count % 10;
  const teens = count % 100 >= 11 && count % 100 <= 14;
  if (ones === 1 && !teens) return "tema";
  return ones >= 2 && ones <= 4 && !teens ? "teme" : "tema";
};
