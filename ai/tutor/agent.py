"""One question: the model reads the material through tools, and its answer is checked."""

import time
from dataclasses import dataclass, field

from pydantic import ValidationError

from tutor.checks import check
from tutor.material import Hit, Material, Period
from tutor.model import Model, ModelReply, ToolChoice
from tutor.prompts import retry_message, system_prompt
from tutor.schemas import Answer, Citation, ModelAnswer, Turn, answer_schema
from tutor.sections import Section, find
from tutor.tools import TOOLS, dispatch

UNVERIFIED = "Nisam uspio potkrijepiti odgovor gradivom. Pokušaj pitati drugačije."


@dataclass
class Stats:
    """What is logged about an answer: numbers and names, never text."""

    kind: str = ""
    period_scoped: bool = False
    tools: list[str] = field(default_factory=list)
    retried: bool = False
    latency_ms: int = 0
    prompt_tokens: int = 0
    completion_tokens: int = 0


@dataclass(frozen=True)
class Outcome:
    answer: Answer
    stats: Stats
    option: int | None = None


class _OneQuestion:
    """The material as it is while one question is answered: each Period is read once, so the
    text the model read is the text its quotes are checked against. Nothing outlives the
    question."""

    def __init__(self, material: Material) -> None:
        self._material = material
        self._sections: dict[int, list[Section] | None] = {}
        self._periods: list[Period] | None = None

    def periods(self) -> list[Period]:
        if self._periods is None:
            self._periods = self._material.periods()
        return self._periods

    def sections(self, period_id: int) -> list[Section] | None:
        if period_id not in self._sections:
            self._sections[period_id] = self._material.sections(period_id)
        return self._sections[period_id]

    def search(self, query: str) -> list[Hit]:
        return self._material.search(query)


def _parse(reply: ModelReply) -> ModelAnswer | None:
    try:
        return ModelAnswer.model_validate_json(reply.content or "")
    except ValidationError:
        return None


def _assistant_message(reply: ModelReply) -> dict:
    return {
        "role": "assistant",
        "content": reply.content,
        "tool_calls": [
            {
                "id": call.id,
                "type": "function",
                "function": {"name": call.name, "arguments": call.arguments},
            }
            for call in reply.tool_calls
        ],
    }


def ask(
    question: str,
    history: list[Turn],
    period_id: int | None,
    model: Model,
    material: Material,
    max_tool_calls: int = 6,
    with_option: bool = False,
) -> Outcome:
    started = time.perf_counter()
    material = _OneQuestion(material)
    stats = Stats()

    periods = material.periods()
    current = next((period for period in periods if period.id == period_id), None)
    outline = material.sections(current.id) if current else None
    stats.period_scoped = bool(outline)

    messages: list[dict] = [
        {"role": "system", "content": system_prompt(periods, current, outline, with_option)},
        *[{"role": turn.role, "content": turn.content} for turn in history],
        {"role": "user", "content": question},
    ]
    schema = answer_schema(with_option)

    def finish(answer: Answer, option: int | None = None) -> Outcome:
        stats.kind = answer.kind
        stats.latency_ms = round((time.perf_counter() - started) * 1000)
        return Outcome(answer, stats, option)

    while True:
        # Asked for an answer in a fixed shape, the model tends to give one at once, from
        # memory. So the first move has to be a tool; after the budget, none may be.
        tool_choice: ToolChoice = (
            "required"
            if not stats.tools
            else "auto"
            if len(stats.tools) < max_tool_calls
            else "none"
        )
        reply = model.complete(messages, TOOLS, schema, tool_choice)
        stats.prompt_tokens += reply.prompt_tokens
        stats.completion_tokens += reply.completion_tokens

        if reply.tool_calls and tool_choice != "none":
            messages.append(_assistant_message(reply))
            for call in reply.tool_calls:
                stats.tools.append(call.name)
                messages.append(
                    {
                        "role": "tool",
                        "tool_call_id": call.id,
                        "content": dispatch(call.name, call.arguments, material),
                    }
                )
            continue

        parsed = _parse(reply)
        failures = (
            ["The reply was not in the required shape."]
            if parsed is None
            else check(parsed, material)
        )
        if parsed is not None and not failures:
            return finish(_named(parsed, material), parsed.option)
        if stats.retried:
            return finish(Answer(kind="unverified", answer=UNVERIFIED, citations=[]))

        stats.retried = True
        messages.append({"role": "assistant", "content": reply.content or ""})
        messages.append({"role": "user", "content": retry_message(failures)})


def _named(parsed: ModelAnswer, material: Material) -> Answer:
    """The checked answer with the names a reader needs beside each citation."""
    names = {period.id: period.name for period in material.periods()}
    citations = []
    for citation in parsed.citations:
        # The check has passed, so the Period and the section exist.
        section = find(material.sections(citation.period_id) or [], citation.section_id)
        citations.append(
            Citation(
                period_id=citation.period_id,
                period_name=names.get(citation.period_id, ""),
                section_id=citation.section_id,
                section_title=section.title if section else "",
                quote=citation.quote,
            )
        )
    return Answer(kind=parsed.kind, answer=parsed.answer, citations=citations)
