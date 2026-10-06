import json

from tests.conftest import QUOTE, RASKOLJNIKOV, ScriptedModel, cite, final, tool
from tutor.agent import UNVERIFIED, ask
from tutor.schemas import Turn


def test_the_model_searches_reads_and_its_quote_is_verified(material):
    model = ScriptedModel(
        tool("search_material", query="Raskoljnikov"),
        tool("read_section", period_id=4, section_id=RASKOLJNIKOV),
        final("answer", "Raskoljnikov je bivši student.", [cite()]),
    )

    outcome = ask("Tko je Raskoljnikov?", [], None, model, material)

    assert outcome.answer.kind == "answer"
    assert outcome.answer.answer == "Raskoljnikov je bivši student."
    citation = outcome.answer.citations[0]
    assert (citation.period_name, citation.section_title, citation.quote) == (
        "Realizam",
        "Fjodor Mihajlovič Dostojevski: Zločin i kazna",
        QUOTE,
    )
    assert outcome.stats.tools == ["search_material", "read_section"]
    assert outcome.stats.retried is False
    assert (outcome.stats.prompt_tokens, outcome.stats.completion_tokens) == (40, 9)
    assert material.searched == ["Raskoljnikov"]

    # Each tool result went back to the model under the call it answers.
    results = [m for m in model.calls[2]["messages"] if m["role"] == "tool"]
    assert [result["tool_call_id"] for result in results] == [
        "call-search_material",
        "call-read_section",
    ]
    assert json.loads(results[0]["content"])["hits"][0]["section_id"] == RASKOLJNIKOV
    assert QUOTE in json.loads(results[1]["content"])["text"]


def test_a_wrong_quote_is_sent_back_once_and_the_second_answer_is_shown(material):
    model = ScriptedModel(
        final("answer", "Prvi.", [cite("Raskoljnikov je bio bogat trgovac iz Moskve.")]),
        final("answer", "Drugi.", [cite()]),
    )

    outcome = ask("Tko je Raskoljnikov?", [], None, model, material)

    assert outcome.answer.answer == "Drugi."
    assert outcome.stats.retried is True
    retry = model.calls[1]["messages"]
    assert retry[-2]["role"] == "assistant"
    assert retry[-1]["role"] == "user"
    assert "the quote was not found in that section" in retry[-1]["content"]
    assert model.calls[1]["tool_choice"] == "required"


def test_two_wrong_quotes_are_never_shown(material):
    wrong = [cite("Raskoljnikov je bio bogat trgovac iz Moskve.")]
    model = ScriptedModel(final("answer", "Prvi.", wrong), final("answer", "Drugi.", wrong))

    outcome = ask("Tko je Raskoljnikov?", [], None, model, material)

    assert outcome.answer.kind == "unverified"
    assert outcome.answer.answer == UNVERIFIED
    assert outcome.answer.citations == []
    assert outcome.option is None


def test_a_reply_out_of_shape_counts_as_a_failed_check(material):
    model = ScriptedModel(final("answer", citations=[cite()]), final("declined"))
    model.replies[0] = type(model.replies[0])(content="not json")

    outcome = ask("Pitanje?", [], None, model, material)

    assert outcome.answer.kind == "declined"
    assert outcome.stats.retried is True


def test_after_six_tool_calls_the_model_must_answer(material):
    model = ScriptedModel(
        *[tool("search_material", query=f"riječ{n}") for n in range(6)],
        final("not_covered", "Gradivo to ne obrađuje."),
    )

    outcome = ask("Pitanje?", [], None, model, material, max_tool_calls=6)

    assert [call["tool_choice"] for call in model.calls] == ["required"] + ["auto"] * 5 + ["none"]
    assert len(outcome.stats.tools) == 6
    assert outcome.answer.kind == "not_covered"


def test_not_covered_and_declined_pass_through(material):
    for kind in ("not_covered", "declined"):
        outcome = ask("Pitanje?", [], None, ScriptedModel(final(kind, "Ne mogu.")), material)

        assert (outcome.answer.kind, outcome.answer.answer) == (kind, "Ne mogu.")
        assert outcome.stats.kind == kind


def test_the_outline_is_in_the_prompt_only_on_a_period_with_content(material):
    def system(period_id):
        model = ScriptedModel(final("declined"))
        outcome = ask("Pitanje?", [], period_id, model, material)
        return model.calls[0]["messages"][0]["content"], outcome.stats.period_scoped

    on_period, scoped = system(4)
    assert "The student is reading: Realizam" in on_period
    assert f"- {RASKOLJNIKOV}: Fjodor Mihajlovič Dostojevski: Zločin i kazna" in on_period
    assert scoped is True

    for period_id in (None, 8, 99):
        elsewhere, scoped = system(period_id)
        assert "The student is reading" not in elsewhere
        assert "- 4: Realizam (1881. – 1892.)" in elsewhere
        assert scoped is False


def test_the_history_is_passed_in_order_before_the_question(material):
    model = ScriptedModel(final("declined"))
    history = [Turn(role="user", content="Prvo."), Turn(role="assistant", content="Drugo.")]

    ask("Treće?", history, None, model, material)

    sent = model.calls[0]["messages"]
    assert [(m["role"], m["content"]) for m in sent[1:]] == [
        ("user", "Prvo."),
        ("assistant", "Drugo."),
        ("user", "Treće?"),
    ]


def test_a_tool_called_wrongly_answers_with_text_the_model_can_read(material):
    model = ScriptedModel(
        tool("read_section", period_id=8, section_id="odjeljak-0"),
        tool("read_section", period_id=4, section_id="odjeljak-77"),
        tool("no_such_tool"),
        final("not_covered"),
    )

    ask("Pitanje?", [], None, model, material)

    results = [m["content"] for m in model.calls[3]["messages"] if m["role"] == "tool"]
    assert all("error" in json.loads(result) for result in results)


def test_the_option_is_asked_for_and_returned_only_when_wanted(material):
    model = ScriptedModel(final("answer", citations=[cite()], option=2))

    outcome = ask("Pitanje s četiri odgovora?", [], 4, model, material, with_option=True)

    assert outcome.option == 2
    assert "option" in model.calls[0]["schema"]["schema"]["properties"]
    assert '"option"' in model.calls[0]["messages"][0]["content"]


def test_the_first_move_must_be_a_tool_and_later_ones_may_be(material):
    model = ScriptedModel(
        tool("search_material", query="Raskoljnikov"),
        final("answer", citations=[cite()]),
    )

    ask("Tko je Raskoljnikov?", [], None, model, material)

    assert [call["tool_choice"] for call in model.calls] == ["required", "auto"]
