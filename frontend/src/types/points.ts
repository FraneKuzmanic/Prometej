// The Croatian word after a number of points: 1 bod, 3 boda, 5 bodova, 21 bod.
export const pointsLabel = (points: number) => {
  const last = points % 10;
  const lastTwo = points % 100;
  if (last === 1 && lastTwo !== 11) return "bod";
  if (last >= 2 && last <= 4 && (lastTwo < 12 || lastTwo > 14)) return "boda";
  return "bodova";
};
