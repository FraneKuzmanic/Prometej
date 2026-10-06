import json

import pytest

from tutor.material import Hit, Period
from tutor.model import ModelReply, ToolCall
from tutor.sections import Section, parse

REALIZAM = """
<h1>Realizam</h1>
<h2>Naziv i vremenski okvir</h2>
<p>Realizam je razdoblje druge polovice 19. stoljeća.</p>
<h2>Djela</h2>
<h3>Fjodor Mihajlovič Dostojevski: Zločin i kazna</h3>
<h4>Kratak sadržaj</h4>
<p>Rodion Romanovič Raskoljnikov bivši je student koji živi u bijedi.</p>
<p>Osuđen je na osam godina robije u Sibiru.</p>
<h3>August Šenoa: Prijan Lovro</h3>
<p>Lovro je sin slovenskih seljaka, iznimno bistar.</p>
"""

RENESANSA = """
<h2>Naziv i vremenski okvir</h2>
<p>Renesansa znači preporod i traje od 14. do 16. stoljeća.</p>
"""

RASKOLJNIKOV = "odjeljak-2"
QUOTE = "Rodion Romanovič Raskoljnikov bivši je student koji živi u bijedi."


class FakeMaterial:
    """Two small Periods with content and one without."""

    def __init__(self) -> None:
        self.texts = {4: REALIZAM, 3: RENESANSA}
        self.searched: list[str] = []

    def periods(self) -> list[Period]:
        return [
            Period(3, "Renesansa", "14. – 16. stoljeće"),
            Period(4, "Realizam", "1881. – 1892."),
            Period(8, "Barok", "17. stoljeće"),
        ]

    def sections(self, period_id: int) -> list[Section] | None:
        html = self.texts.get(period_id)
        return None if html is None else parse(html)

    def search(self, query: str) -> list[Hit]:
        self.searched.append(query)
        hits = []
        for period in self.periods():
            for section in self.sections(period.id) or []:
                for line in section.text.split("\n"):
                    if query.lower() in line.lower():
                        hits.append(Hit(period.id, period.name, section.id, section.title, line))
        return hits


class ScriptedModel:
    """Returns its replies in order and keeps what it was sent."""

    def __init__(self, *replies: ModelReply) -> None:
        self.replies = list(replies)
        self.calls: list[dict] = []

    def complete(self, messages, tools, schema, allow_tools) -> ModelReply:
        self.calls.append(
            {
                "messages": [dict(message) for message in messages],
                "tools": tools,
                "schema": schema,
                "allow_tools": allow_tools,
            }
        )
        return self.replies.pop(0)


def tool(name: str, **arguments) -> ModelReply:
    call = ToolCall(id=f"call-{name}", name=name, arguments=json.dumps(arguments))
    return ModelReply(content=None, tool_calls=[call], prompt_tokens=10, completion_tokens=2)


def final(kind: str, answer: str = "Odgovor.", citations: list[dict] | None = None, **more):
    body = {"kind": kind, "answer": answer, "citations": citations or [], **more}
    return ModelReply(
        content=json.dumps(body, ensure_ascii=False), prompt_tokens=20, completion_tokens=5
    )


def cite(quote: str = QUOTE, period_id: int = 4, section_id: str = RASKOLJNIKOV) -> dict:
    return {"period_id": period_id, "section_id": section_id, "quote": quote}


@pytest.fixture
def material() -> FakeMaterial:
    return FakeMaterial()
