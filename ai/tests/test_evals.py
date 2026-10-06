from evals.report import table
from evals.run import choice_items, summary
from evals.score import choice_right, coverage_right, refusal_right
from tests.conftest import REALIZAM
from tutor.schemas import Citation
from tutor.sections import parse

SECTIONS = parse(REALIZAM)
WORK = "Fjodor Mihajlovič Dostojevski: Zločin i kazna"


def cited(section_id: str, period_id: int = 4) -> list[Citation]:
    return [
        Citation(
            period_id=period_id,
            period_name="Realizam",
            section_id=section_id,
            section_title="",
            quote="",
        )
    ]


def test_a_choice_is_right_when_the_option_is_the_key():
    assert choice_right(3, 3)
    assert not choice_right(3, 2)
    # An answer that failed the quote check has no option.
    assert not choice_right(3, None)


def test_coverage_is_right_in_the_named_section_its_part_or_its_chapter():
    assert coverage_right(4, [WORK], cited("odjeljak-2"), SECTIONS)
    # The chapter of the named part: reading "Djela" returns the work.
    assert coverage_right(4, [WORK], cited("odjeljak-1"), SECTIONS)
    # A part of the named chapter.
    assert coverage_right(4, ["Djela"], cited("odjeljak-3"), SECTIONS)


def test_coverage_is_wrong_elsewhere():
    assert not coverage_right(4, [WORK], cited("odjeljak-3"), SECTIONS)
    assert not coverage_right(4, [WORK], cited("odjeljak-0"), SECTIONS)
    assert not coverage_right(4, [WORK], cited("odjeljak-2", period_id=3), SECTIONS)
    assert not coverage_right(4, [WORK], cited("odjeljak-99"), SECTIONS)
    assert not coverage_right(4, [WORK], [], SECTIONS)


def test_a_refusal_is_anything_but_an_answer():
    assert refusal_right("not_covered")
    assert refusal_right("declined")
    assert refusal_right("unverified")
    assert not refusal_right("answer")


def test_set_a_is_the_demo_choice_questions_that_are_not_about_a_poem():
    items = choice_items()

    assert len(items) == 24
    assert {item["periodId"] for item in items} == {3, 4, 6}
    first = next(item for item in items if item["id"].startswith("realizam"))
    assert first["question"].count("\n") == 4
    assert "\n3. Fjodor Mihajlovič Dostojevski" in first["question"]
    assert first["correctOption"] == 3


def test_the_summary_counts_right_retried_and_unverified():
    results = [
        {
            "right": True,
            "kind": "answer",
            "retried": True,
            "prompt_tokens": 5,
            "completion_tokens": 1,
        },
        {
            "right": False,
            "kind": "unverified",
            "retried": True,
            "prompt_tokens": 5,
            "completion_tokens": 1,
        },
        {
            "right": True,
            "kind": "answer",
            "retried": False,
            "prompt_tokens": 5,
            "completion_tokens": 1,
        },
    ]

    assert summary(results) == {
        "items": 3,
        "right": 2,
        "share": 0.6667,
        "retried": 2,
        "unverified": 1,
        "prompt_tokens": 15,
        "completion_tokens": 3,
    }


def test_the_report_says_miss_where_a_bar_is_missed():
    bars = {
        "choice": {"bar": 0.9},
        "choice-baseline": {"bar": None},
        "grounding": {"bar": 0.1},
        "coverage": {"bar": 0.85},
        "refusal": {"bar": 0.9},
    }

    def record(name: str, right: int, items: int) -> dict:
        totals = {
            "items": items,
            "right": right,
            "share": right / items,
            "retried": 1,
            "unverified": 0,
        }
        return {"set": name, "date": "2026-10-06", "summary": totals}

    lines = table(
        bars,
        [record("choice", 23, 24), record("choice-baseline", 22, 24), record("coverage", 20, 36)],
    ).splitlines()

    assert "| 23 (95.8%) | pass |" in lines[2]
    assert "| 22 (91.7%) |  |" in lines[3]
    assert "| 20 (55.6%) | **miss** |" in lines[4]
    assert "+4.2 points | **miss** |" in lines[5]
