import json
from pathlib import Path

from tutor.sections import parse

ROOT = Path(__file__).resolve().parents[2]
SETS = ROOT / "ai/evals/sets"
PERIODS = ROOT / "backend/Prometej_api/Seed/Content/periods"


def rows(name: str) -> list[dict]:
    lines = (SETS / name).read_text(encoding="utf-8").splitlines()
    return [json.loads(line) for line in lines if line.strip()]


def headings(period_id: int) -> set[str]:
    (file,) = PERIODS.glob(f"{period_id}-*.html")
    return {section.title for section in parse(file.read_text(encoding="utf-8"))}


def test_every_expected_heading_is_a_section_of_its_period():
    for row in rows("coverage.jsonl"):
        missing = set(row["headings"]) - headings(row["periodId"])
        assert not missing, f"{row['id']}: {missing}"


def test_the_coverage_set_reaches_all_twelve_periods():
    assert {row["periodId"] for row in rows("coverage.jsonl")} == set(range(1, 13))


def test_ids_are_unique_and_every_refusal_says_what_it_expects():
    for name in ("dev.jsonl", "coverage.jsonl", "refusal.jsonl"):
        ids = [row["id"] for row in rows(name)]
        assert len(ids) == len(set(ids)), name
    assert {row["expect"] for row in rows("refusal.jsonl")} == {"not_covered", "declined"}
