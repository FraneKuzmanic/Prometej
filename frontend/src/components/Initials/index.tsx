// A person's initials on a disc, as the header shows the signed-in User. A name that is
// missing (the account was deleted) gets a dash.
export default function Initials({ name, small }: { name: string | null; small?: boolean }) {
  const initials = (name ?? "")
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((word) => word[0].toUpperCase())
    .join("");
  return (
    <span className={small ? "initials small" : "initials"} aria-hidden="true">
      {initials || "–"}
    </span>
  );
}
