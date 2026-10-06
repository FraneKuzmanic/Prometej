"""The one bar a script cannot score: would a teacher use the draft.

    python -m evals.drafts_rate --sample    writes results/drafts-to-rate.md
    python -m evals.drafts_rate --score     reads the ratings back

A person writes "da" (usable as it is, or after changing a word or a comma) or "ne" on each
"ocjena:" line. The sample is drawn with a fixed seed from the drafts that were offered.
"""

import argparse
import json
import random
import re
import sys

from evals.run import RESULTS
from tutor.questions import OPTION_KEYS

SAMPLE_SIZE = 30
SEED = 16
SHEET = RESULTS / "drafts-to-rate.md"
RATED = RESULTS / "drafts-rated.json"

INTRO = """\
# Prijedlozi pitanja za ocjenu

Uz svaki prijedlog upiši `da` ili `ne` iza `ocjena:`.

- `da`: pitanje bih stavio u kviz ovakvo kakvo jest, ili uz sitnu izmjenu (riječ, zarez).
- `ne`: pitanje je netočno, nejasno, prelagano ili bi mu trebalo napisati nov odgovor.

Točan odgovor označen je s `(točan)`. Navod je rečenica iz gradiva na kojoj pitanje počiva.
"""


def latest_run() -> dict:
    files = sorted(RESULTS.glob("*-drafts.json"))
    if not files:
        sys.exit("No drafts run is recorded. Run python -m evals.drafts_run first.")
    return json.loads(files[-1].read_text(encoding="utf-8"))


def sample(items: list[dict]) -> list[dict]:
    kept = [item for item in items if item["valid"] and item["quoteFound"]]
    chosen = random.Random(SEED).sample(kept, min(SAMPLE_SIZE, len(kept)))
    return sorted(chosen, key=lambda item: (item["periodId"], item["id"]))


def sheet(items: list[dict]) -> str:
    parts = [INTRO]
    for number, item in enumerate(items, start=1):
        options = "\n".join(
            f"{n}. {item[key]}{' (točan)' if n == item['correctOption'] else ''}"
            for n, key in enumerate(OPTION_KEYS, start=1)
        )
        parts.append(
            f"## {number}. {item['periodName']} · {item['sectionTitle']}\n\n"
            f"id: {item['id']}\n\n"
            f"**{item['questionTitle']}**\n\n{options}\n\n"
            f"> {item['quote']}\n\n"
            f"ocjena: \n"
        )
    return "\n".join(parts)


def read_ratings(text: str) -> dict[str, str]:
    """The rating under each id; a line left empty or with anything else is an error."""
    ratings: dict[str, str] = {}
    current = None
    for line in text.splitlines():
        if match := re.fullmatch(r"id:\s*(\S+)", line.strip()):
            current = match.group(1)
        elif line.strip().lower().startswith("ocjena:") and current is not None:
            value = line.split(":", 1)[1].strip().lower()
            if value not in ("da", "ne"):
                raise ValueError(f"{current}: the rating is '{value}', not da or ne")
            ratings[current] = value
            current = None
    return ratings


def main() -> None:
    sys.stdout.reconfigure(encoding="utf-8")
    parser = argparse.ArgumentParser()
    action = parser.add_mutually_exclusive_group(required=True)
    action.add_argument("--sample", action="store_true")
    action.add_argument("--score", action="store_true")
    args = parser.parse_args()

    if args.sample:
        if SHEET.exists():
            sys.exit(f"{SHEET.name} exists. It may hold ratings; it is not overwritten.")
        items = sample(latest_run()["items"])
        SHEET.write_text(sheet(items), encoding="utf-8")
        print(f"{len(items)} drafts to rate: {SHEET}")
        return

    try:
        ratings = read_ratings(SHEET.read_text(encoding="utf-8"))
    except ValueError as error:
        sys.exit(str(error))
    usable = sum(value == "da" for value in ratings.values())
    record = {
        "set": "drafts-rated",
        "rated": len(ratings),
        "usable": usable,
        "usable_share": round(usable / len(ratings), 4) if ratings else None,
        "ratings": ratings,
    }
    RATED.write_text(json.dumps(record, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({key: record[key] for key in ("rated", "usable", "usable_share")}))


if __name__ == "__main__":
    main()
