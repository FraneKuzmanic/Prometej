import pytest

from evals.drafts_rate import read_ratings, sample, sheet
from evals.drafts_run import first_work, summary
from evals.report import drafts_table
from tests.conftest import REALIZAM
from tutor.sections import parse


def item(number: int, valid: bool = True, quote_found: bool = True, agrees: bool | None = True):
    return {
        "id": f"p4-{number}",
        "periodId": 4,
        "periodName": "Realizam",
        "sectionTitle": "Zločin i kazna",
        "questionTitle": f"Pitanje {number}?",
        "firstAnswer": "A",
        "secondAnswer": "B",
        "thirdAnswer": "C",
        "fourthAnswer": "D",
        "correctOption": 2,
        "quote": "Rečenica iz gradiva.",
        "valid": valid,
        "quoteFound": quote_found,
        "agrees": agrees,
    }


def test_the_section_drafted_from_is_the_first_work_of_a_period():
    assert first_work(parse(REALIZAM)).title == "Fjodor Mihajlovič Dostojevski: Zločin i kazna"
    assert first_work(parse("<h2>Sažetak</h2><p>Tekst.</p>")) is None


def test_the_summary_counts_every_draft_written_and_agreement_among_those_kept():
    items = [
        item(1),
        item(2, agrees=False),
        item(3, valid=False, agrees=None),
        item(4, quote_found=False, agrees=None),
    ]

    totals = summary(items, 100, 10)

    assert (totals["written"], totals["valid"], totals["quote_found"]) == (4, 3, 3)
    assert (totals["kept"], totals["agrees"], totals["agrees_share"]) == (2, 1, 0.5)
    assert (totals["valid_share"], totals["quote_found_share"]) == (0.75, 0.75)


def test_the_sample_is_thirty_offered_drafts_and_the_same_every_time():
    items = [item(n) for n in range(50)] + [item(99, valid=False, agrees=None)]

    first, second = sample(items), sample(items)

    assert len(first) == 30
    assert first == second
    assert all(chosen["valid"] for chosen in first)


def test_ratings_are_read_back_from_the_sheet():
    text = sheet([item(1), item(2)])
    assert "2. B (točan)" in text

    rated = text.replace("ocjena: ", "ocjena: da", 1).replace("ocjena: \n", "ocjena: NE\n")

    assert read_ratings(rated) == {"p4-1": "da", "p4-2": "ne"}


def test_a_rating_left_empty_is_an_error():
    with pytest.raises(ValueError, match="p4-1"):
        read_ratings(sheet([item(1)]))


def test_the_drafts_table_marks_a_miss():
    bars = {
        "drafts-valid": {"bar": 0.95},
        "drafts-quote": {"bar": 0.95},
        "drafts-agrees": {"bar": 0.85},
        "drafts-usable": {"bar": 0.7},
    }
    run = {"summary": {"written": 60, "valid": 60, "quote_found": 54, "kept": 54, "agrees": 50}}

    lines = drafts_table(bars, run, {"rated": 30, "usable": 20}).splitlines()

    assert "| 60 (100.0%) | pass |" in lines[2]
    assert "| 54 (90.0%) | **miss** |" in lines[3]
    assert "| 54 | at least 85.0% | 50 (92.6%) | pass |" in lines[4]
    assert "| 30 | at least 70.0% | 20 (66.7%) | **miss** |" in lines[5]
