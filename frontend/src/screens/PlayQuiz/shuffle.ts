// The numbers 0 to count - 1 in a random order (Fisher-Yates).
const shuffled = (count: number) => {
  const order = Array.from({ length: count }, (_, i) => i);
  for (let i = count - 1; i > 0; i--) {
    const j = Math.floor(Math.random() * (i + 1));
    [order[i], order[j]] = [order[j], order[i]];
  }
  return order;
};

// The same, but never 0, 1, 2…: shown in their stored order, the items of an ordering
// Question, or the right-hand options of a matching one, would give the answer away.
export const shuffledOutOfOrder = (count: number) => {
  let order = shuffled(count);
  while (count > 1 && order.every((value, i) => value === i)) {
    order = shuffled(count);
  }
  return order;
};
