import json

import pytest
from fastapi.testclient import TestClient

from tests.conftest import QUOTE, RASKOLJNIKOV, ScriptedModel
from tutor.app import create_app
from tutor.drafts import UnknownSection, draft
from tutor.model import ModelReply


def written(**changes) -> dict:
    return {
        "question_title": "Tko je Raskoljnikov?",
        "first_answer": "Bivši student",
        "second_answer": "Istražitelj",
        "third_answer": "Trgovac",
        "fourth_answer": "Liječnik",
        "correct_option": 1,
        "quote": QUOTE,
        **changes,
    }


def drafts(*items: dict) -> ModelReply:
    body = json.dumps({"drafts": list(items)}, ensure_ascii=False)
    return ModelReply(content=body, prompt_tokens=100, completion_tokens=40)


def picks(option: int) -> ModelReply:
    return ModelReply(content=json.dumps({"option": option}), prompt_tokens=50, completion_tokens=1)


def test_a_draft_that_holds_is_kept_and_asked_once_more(material):
    model = ScriptedModel(drafts(written()), picks(1))

    drafted = draft(4, RASKOLJNIKOV, model, material)

    (kept,) = drafted.kept
    assert (kept.valid, kept.quote_found, kept.agrees) == (True, True, True)
    assert drafted.dropped == 0
    assert (drafted.prompt_tokens, drafted.completion_tokens) == (150, 41)
    # The section's text is what the model writes from, and no tool is offered.
    first, second = model.calls
    assert QUOTE in first["messages"][1]["content"]
    assert (first["tools"], first["tool_choice"]) == ([], "none")
    # The second reading gets the options and not which of them the draft marks.
    asked = second["messages"][1]["content"]
    assert "1. Bivši student" in asked
    assert "4. Liječnik" in asked
    assert "correct" not in asked


def test_a_draft_the_api_would_refuse_or_with_a_false_quote_is_dropped(material):
    model = ScriptedModel(
        drafts(
            written(),
            written(second_answer="Bivši student"),
            written(quote="Raskoljnikov je bio imućan trgovac iz Moskve."),
            written(correct_option=7),
        ),
        picks(1),
    )

    drafted = draft(4, RASKOLJNIKOV, model, material)

    assert len(drafted.kept) == 1
    assert drafted.dropped == 3
    assert [(j.valid, j.quote_found, j.agrees) for j in drafted.judged[1:]] == [
        (False, True, None),
        (True, False, None),
        (False, True, None),
    ]
    # A dropped draft is not asked a second time.
    assert len(model.calls) == 2


def test_a_second_reading_that_disagrees_only_warns(material):
    model = ScriptedModel(drafts(written()), picks(3))

    drafted = draft(4, RASKOLJNIKOV, model, material)

    (kept,) = drafted.kept
    assert kept.agrees is False


def test_no_more_than_five_drafts_are_read(material):
    model = ScriptedModel(drafts(*[written() for _ in range(7)]), *[picks(1) for _ in range(5)])

    assert len(draft(4, RASKOLJNIKOV, model, material).judged) == 5


def test_a_reply_out_of_shape_gives_no_drafts(material):
    drafted = draft(4, RASKOLJNIKOV, ScriptedModel(ModelReply(content="not json")), material)

    assert drafted.judged == []


@pytest.mark.parametrize(("period_id", "section_id"), [(4, "odjeljak-99"), (8, "odjeljak-0")])
def test_a_section_that_does_not_exist_is_refused(material, period_id, section_id):
    with pytest.raises(UnknownSection):
        draft(period_id, section_id, ScriptedModel(), material)


def test_the_service_answers_the_kept_drafts_in_camel_case(material, caplog):
    model = ScriptedModel(drafts(written(), written(quote="Ovoga u tekstu nema nigdje.")), picks(2))
    client = TestClient(create_app(model, material))

    with caplog.at_level("INFO", logger="tutor"):
        response = client.post("/drafts", json={"periodId": 4, "sectionId": RASKOLJNIKOV})

    assert response.status_code == 200
    assert response.json() == {
        "drafts": [
            {
                "questionTitle": "Tko je Raskoljnikov?",
                "firstAnswer": "Bivši student",
                "secondAnswer": "Istražitelj",
                "thirdAnswer": "Trgovac",
                "fourthAnswer": "Liječnik",
                "correctOption": 1,
                "quote": QUOTE,
                "agrees": False,
            }
        ],
        "dropped": 1,
    }
    line = json.loads(caplog.records[-1].getMessage())
    assert (line["drafts"], line["dropped"], line["disagreed"]) == (1, 1, 1)
    assert "Raskoljnikov" not in caplog.text


def test_the_service_says_404_for_an_unknown_section(material):
    client = TestClient(create_app(ScriptedModel(), material))
    response = client.post("/drafts", json={"periodId": 4, "sectionId": "odjeljak-99"})

    assert response.status_code == 404
