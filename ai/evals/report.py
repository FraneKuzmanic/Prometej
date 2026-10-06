"""Prints the recorded results as the Markdown table the README shows.

python -m evals.report
"""

import json
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
ORDER = ["choice", "choice-baseline", "coverage", "refusal"]
NAMES = {
    "choice": "A. Quiz questions, with tools",
    "choice-baseline": "A. The same questions, no tools (baseline)",
    "coverage": "B. Where is this covered",
    "refusal": "C. Not covered, or homework",
}


def percent(share: float) -> str:
    return f"{share * 100:.1f}%"


def verdict(share: float, bar: float | None) -> str:
    if bar is None:
        return ""
    return "pass" if share >= bar else "**miss**"


def table(bars: dict, records: list[dict]) -> str:
    lines = [
        "| Set | Items | Bar | Result | | Needed the retry | Never shown |",
        "| --- | --- | --- | --- | --- | --- | --- |",
    ]
    shares: dict[str, float] = {}
    for record in sorted(records, key=lambda r: (ORDER.index(r["set"]), r["date"])):
        name, totals = record["set"], record["summary"]
        bar = bars[name]["bar"]
        shares[name] = totals["share"]
        agent_run = name != "choice-baseline"
        lines.append(
            f"| {NAMES[name]} | {totals['items']} | "
            f"{'at least ' + percent(bar) if bar is not None else ''} | "
            f"{totals['right']} ({percent(totals['share'])}) | {verdict(totals['share'], bar)} | "
            f"{totals['retried'] if agent_run else ''} | "
            f"{totals['unverified'] if agent_run else ''} |"
        )
    if "choice" in shares and "choice-baseline" in shares:
        added = shares["choice"] - shares["choice-baseline"]
        bar = bars["grounding"]["bar"]
        lines.append(
            f"| A. What the tools add | | at least {bar * 100:.0f} points | "
            f"{added * 100:+.1f} points | {verdict(added, bar)} | | |"
        )
    return "\n".join(lines)


def main() -> None:
    sys.stdout.reconfigure(encoding="utf-8")
    bars = json.loads((HERE / "bars.json").read_text(encoding="utf-8"))
    records = [
        json.loads(file.read_text(encoding="utf-8"))
        for file in sorted((HERE / "results").glob("*.json"))
    ]
    records = [r for r in records if r.get("set") in ORDER and r.get("limit") is None]
    print(table(bars, records))
    tokens = sum(r["summary"]["prompt_tokens"] + r["summary"]["completion_tokens"] for r in records)
    print(f"\nTokens, all recorded runs: {tokens}")


if __name__ == "__main__":
    main()
