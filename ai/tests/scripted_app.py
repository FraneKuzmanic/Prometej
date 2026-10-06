"""The service with a scripted model and the real material, for the browser walk-through.

    uvicorn tests.scripted_app:app --host 127.0.0.1 --port 8000

A browser check that spends money and can differ from run to run would not be run. The model
here is a few fixed moves chosen by the question's words; the tools, the material and the
quote check are the real ones.
"""

import json

from tutor.app import create_app
from tutor.model import ModelReply, ToolCall
from tutor.sections import clean_text

FALSE_QUOTE = "Raskoljnikov je bio imućan trgovac koji je cijeli život proveo u Moskvi."


def _final(kind: str, answer: str, citations: list[dict] | None = None) -> ModelReply:
    body = {"kind": kind, "answer": answer, "citations": citations or []}
    return ModelReply(content=json.dumps(body, ensure_ascii=False))


def _call(name: str, **arguments) -> ModelReply:
    call = ToolCall(id=f"call-{name}", name=name, arguments=json.dumps(arguments))
    return ModelReply(content=None, tool_calls=[call])


def _drafts(source: str) -> ModelReply:
    """Three drafts from the section sent: two whose quote is its first lines, one whose
    quote is in no text and is dropped."""
    lines = [line for line in source.split("\n\n", 1)[1].split("\n") if len(line) >= 40]

    def one(title: str, quote: str) -> dict:
        return {
            "question_title": title,
            "first_answer": "Prvi odgovor",
            "second_answer": "Drugi odgovor",
            "third_answer": "Treći odgovor",
            "fourth_answer": "Četvrti odgovor",
            "correct_option": 1,
            "quote": quote,
        }

    drafts = [
        one("Prvi prijedlog pitanja?", clean_text(lines[0])[:200].rsplit(" ", 1)[0]),
        one("Drugi prijedlog pitanja?", clean_text(lines[1])[:200].rsplit(" ", 1)[0]),
        one("Treći prijedlog pitanja?", FALSE_QUOTE),
    ]
    return ModelReply(content=json.dumps({"drafts": drafts}, ensure_ascii=False))


class WalkthroughModel:
    def complete(self, messages, tools, schema, tool_choice) -> ModelReply:
        if schema["name"] == "question_drafts":
            return _drafts(messages[1]["content"])
        if schema["name"] == "option":
            # The second reading disagrees with the second draft.
            disagrees = "Drugi prijedlog pitanja?" in messages[1]["content"]
            return ModelReply(content=json.dumps({"option": 2 if disagrees else 1}))

        question = next(m["content"] for m in reversed(messages) if m["role"] == "user")
        question = next(
            (
                m["content"]
                for m in reversed(messages)
                if m["role"] == "user" and not m["content"].startswith("Your answer was not shown")
            ),
            question,
        ).lower()
        results = [json.loads(m["content"]) for m in messages if m["role"] == "tool"]

        if "sastavak" in question:
            return _final(
                "declined",
                "Sastavak ti ne mogu napisati, jer ga trebaš napisati ti. Mogu ti pokazati "
                "gdje u gradivu piše o tom djelu.",
            )
        if "trgovac" in question:
            # A quote that is in no text, both times: the answer must never be shown.
            return _final(
                "answer",
                "Raskoljnikov je bio trgovac.",
                [{"period_id": 4, "section_id": "odjeljak-0", "quote": FALSE_QUOTE}],
            )
        if "raskoljnikov" in question:
            if not results:
                return _call("search_material", query="Raskoljnikov")
            if len(results) == 1:
                hit = next(h for h in results[0]["hits"] if h["section_id"])
                return _call(
                    "read_section", period_id=hit["period_id"], section_id=hit["section_id"]
                )
            # The quote is read from the material now, so it holds when a text is edited.
            section = results[1]
            line = next(line for line in section["text"].split("\n") if "Raskoljnikov" in line)
            quote = clean_text(line)[:200].rsplit(" ", 1)[0]
            return _final(
                "answer",
                "Raskoljnikov je glavni lik romana Zločin i kazna. Pročitaj kratak sadržaj djela.",
                [
                    {
                        "period_id": section["period_id"],
                        "section_id": section["section_id"],
                        "quote": quote,
                    }
                ],
            )
        return _final("not_covered", "U gradivu o tome ne piše.")


app = create_app(model=WalkthroughModel())
