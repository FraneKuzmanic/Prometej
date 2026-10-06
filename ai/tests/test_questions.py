import json
from pathlib import Path

import pytest

from tutor.questions import problems

CASES = json.loads(
    (Path(__file__).parent / "fixtures/question_cases.json").read_text(encoding="utf-8")
)


@pytest.mark.parametrize("case", CASES, ids=[case["name"] for case in CASES])
def test_a_question_is_judged_as_the_api_judges_it(case):
    assert (problems(case["question"]) == []) is case["valid"]


def test_the_cases_hold_both_kinds():
    assert {case["valid"] for case in CASES} == {True, False}
