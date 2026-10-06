import json
import logging

import httpx
from fastapi.testclient import TestClient

from tests.conftest import QUOTE, RASKOLJNIKOV, ScriptedModel, cite, final, tool
from tutor.app import create_app

QUESTION = "Tko je Raskoljnikov iz Zločina i kazne?"


def test_health_needs_no_model_and_no_material():
    response = TestClient(create_app()).get("/health")

    assert response.status_code == 200
    assert response.json() == {"status": "ok"}


def test_an_answer_goes_out_in_camel_case(material):
    model = ScriptedModel(final("answer", "Bivši student.", [cite()]))
    client = TestClient(create_app(model, material))

    response = client.post(
        "/ask",
        json={
            "question": QUESTION,
            "periodId": 4,
            "history": [{"role": "user", "content": "Bok."}],
        },
    )

    assert response.status_code == 200
    assert response.json() == {
        "kind": "answer",
        "answer": "Bivši student.",
        "citations": [
            {
                "periodId": 4,
                "periodName": "Realizam",
                "sectionId": RASKOLJNIKOV,
                "sectionTitle": "Fjodor Mihajlovič Dostojevski: Zločin i kazna",
                "quote": QUOTE,
            }
        ],
    }
    # periodId was read: the Period's outline is in the prompt.
    assert "The student is reading: Realizam" in model.calls[0]["messages"][0]["content"]
    assert model.calls[0]["messages"][1]["content"] == "Bok."


def test_the_log_line_holds_numbers_and_no_text(material, caplog):
    model = ScriptedModel(
        tool("search_material", query="Raskoljnikov"),
        final("answer", "Bivši student koji živi u bijedi.", [cite()]),
    )
    client = TestClient(create_app(model, material))

    with caplog.at_level(logging.INFO, logger="tutor"):
        client.post("/ask", json={"question": QUESTION})

    line = json.loads(caplog.records[-1].getMessage())
    assert line == {
        "kind": "answer",
        "period_scoped": False,
        "tools": ["search_material"],
        "retried": False,
        "latency_ms": line["latency_ms"],
        "prompt_tokens": 30,
        "completion_tokens": 7,
    }
    assert "Raskoljnikov" not in caplog.text
    assert "bijedi" not in caplog.text


def test_a_model_or_a_material_that_fails_is_502(material):
    class Failing:
        def complete(self, messages, tools, schema, tool_choice):
            raise httpx.ConnectError("no route")

    response = TestClient(create_app(Failing(), material)).post("/ask", json={"question": "?"})

    assert response.status_code == 502


def test_a_request_without_a_question_is_refused(material):
    client = TestClient(create_app(ScriptedModel(), material))

    assert client.post("/ask", json={"question": ""}).status_code == 422
    assert client.post("/ask", json={}).status_code == 422
