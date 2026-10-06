import pytest

from tests.conftest import QUOTE, RASKOLJNIKOV, cite
from tutor.checks import check
from tutor.schemas import ModelAnswer, answer_schema


def answer(kind: str = "answer", *citations: dict) -> ModelAnswer:
    return ModelAnswer(kind=kind, answer="Odgovor.", citations=list(citations))


def test_a_quote_from_the_section_passes(material):
    assert check(answer("answer", cite()), material) == []


def test_a_quote_broken_by_a_line_break_is_found(material):
    broken = QUOTE.replace(" bivši", "\n  bivši")

    assert check(answer("answer", cite(broken)), material) == []


def test_a_quote_over_two_paragraphs_is_found(material):
    two = "koji živi u bijedi. Osuđen je na osam godina robije u Sibiru."

    assert check(answer("answer", cite(two)), material) == []


def test_a_quote_from_a_part_is_found_under_its_chapter(material):
    assert check(answer("answer", cite(section_id="odjeljak-1")), material) == []


@pytest.mark.parametrize(
    ("citation", "reason"),
    [
        (cite(QUOTE.replace("bivši", "bivsi")), "was not found"),
        (cite(QUOTE.replace("student", "Student")), "was not found"),
        (cite(QUOTE.replace(".", "!")), "was not found"),
        (cite("živi u bijedi"), "15 to 400 characters"),
        (cite(section_id="odjeljak-3"), "was not found"),
        (cite(section_id="odjeljak-99"), "does not exist"),
        (cite(period_id=8), "has no material"),
        (cite(period_id=99), "has no material"),
        (cite(period_id=3, section_id=RASKOLJNIKOV), "does not exist"),
    ],
)
def test_a_citation_that_does_not_hold_fails_with_its_reason(material, citation, reason):
    failures = check(answer("answer", citation), material)

    assert len(failures) == 1
    assert reason in failures[0]


def test_an_answer_needs_a_citation(material):
    assert "needs at least one citation" in check(answer("answer"), material)[0]


def test_not_covered_and_declined_need_none(material):
    assert check(answer("not_covered"), material) == []
    assert check(answer("declined"), material) == []


def test_a_citation_of_any_kind_of_answer_is_checked(material):
    assert check(answer("not_covered", cite("Ovoga u tekstu sigurno nema.")), material) != []


@pytest.mark.parametrize("with_option", [False, True])
def test_the_schema_is_strict(with_option):
    schema = answer_schema(with_option)["schema"]
    citation = schema["properties"]["citations"]["items"]

    assert answer_schema(with_option)["strict"] is True
    for part in (schema, citation):
        assert part["additionalProperties"] is False
        assert part["required"] == list(part["properties"])
    assert ("option" in schema["properties"]) is with_option
