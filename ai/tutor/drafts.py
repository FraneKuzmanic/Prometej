"""Drafts of four-option Questions from one section, for a Teacher to keep, change or drop.

Two checks decide whether a draft is offered at all: the API would accept it, and its quote
is in the section. A third only warns: asked the Question with nothing but the text, does the
model pick the option the draft says is right.
"""

import json
from dataclasses import dataclass, field

from pydantic import BaseModel, ValidationError

from tutor.checks import MAX_QUOTE, MIN_QUOTE, normalise
from tutor.material import Material
from tutor.model import Model
from tutor.questions import OPTION_KEYS, problems
from tutor.sections import chapter_text, find

MAX_DRAFTS = 5

DRAFT_PROMPT = f"""\
You write multiple-choice questions for Croatian high-school students, from one section of \
their learning material about literature.

Write up to {MAX_DRAFTS} questions, in Croatian, that a student can answer from this text \
alone. Ask about what matters in the text: who, what, why, which work, which feature. Do not \
ask about the wording of the text itself.

- Each question has four answers: one right by the text, three wrong but plausible. All four \
differ, and none is "all of the above" or "none of the above".
- The place of the right answer varies from question to question.
- For each question give the quote: the one or two whole sentences of the text that make the \
right answer right, copied exactly, character for character, at most 300 characters.
- If the text is too short for {MAX_DRAFTS} good questions, write fewer.
"""

SECOND_PROMPT = (
    "Answer the multiple-choice question from the text only. "
    'Put the number of the right answer in "option".'
)

_DRAFT = {
    "type": "object",
    "properties": {
        "question_title": {"type": "string"},
        "first_answer": {"type": "string"},
        "second_answer": {"type": "string"},
        "third_answer": {"type": "string"},
        "fourth_answer": {"type": "string"},
        "correct_option": {"type": "integer", "enum": [1, 2, 3, 4]},
        "quote": {"type": "string"},
    },
    "additionalProperties": False,
}
_DRAFT["required"] = list(_DRAFT["properties"])

DRAFTS_SCHEMA = {
    "name": "question_drafts",
    "strict": True,
    "schema": {
        "type": "object",
        "properties": {"drafts": {"type": "array", "items": _DRAFT}},
        "required": ["drafts"],
        "additionalProperties": False,
    },
}

OPTION_SCHEMA = {
    "name": "option",
    "strict": True,
    "schema": {
        "type": "object",
        "properties": {"option": {"type": "integer", "enum": [1, 2, 3, 4]}},
        "required": ["option"],
        "additionalProperties": False,
    },
}


class UnknownSection(Exception):
    pass


class ModelDraft(BaseModel):
    question_title: str
    first_answer: str
    second_answer: str
    third_answer: str
    fourth_answer: str
    correct_option: int
    quote: str

    def question(self) -> dict:
        """The draft in the shape `quiz/create` takes a Question."""
        return {
            "questionTitle": self.question_title,
            "firstAnswer": self.first_answer,
            "secondAnswer": self.second_answer,
            "thirdAnswer": self.third_answer,
            "fourthAnswer": self.fourth_answer,
            "correctOption": self.correct_option,
        }


class ModelDrafts(BaseModel):
    drafts: list[ModelDraft]


@dataclass(frozen=True)
class Judged:
    """One draft as the model wrote it, with what each check said."""

    draft: ModelDraft
    valid: bool
    quote_found: bool
    # None for a draft that is not offered: it is not asked a second time.
    agrees: bool | None

    @property
    def kept(self) -> bool:
        return self.valid and self.quote_found


@dataclass
class Drafted:
    judged: list[Judged] = field(default_factory=list)
    prompt_tokens: int = 0
    completion_tokens: int = 0

    @property
    def kept(self) -> list[Judged]:
        return [judged for judged in self.judged if judged.kept]

    @property
    def dropped(self) -> int:
        return len(self.judged) - len(self.kept)


def _quote_found(quote: str, text: str) -> bool:
    quote = normalise(quote)
    return MIN_QUOTE <= len(quote) <= MAX_QUOTE and quote in normalise(text)


def draft(period_id: int, section_id: str, model: Model, material: Material) -> Drafted:
    sections = material.sections(period_id)
    section = find(sections or [], section_id)
    if sections is None or section is None:
        raise UnknownSection
    text = chapter_text(sections, section_id) or ""
    source = f"Section: {section.title}\n\n{text}"

    result = Drafted()

    def complete(system: str, user: str, schema: dict) -> str:
        reply = model.complete(
            [{"role": "system", "content": system}, {"role": "user", "content": user}],
            [],
            schema,
            "none",
        )
        result.prompt_tokens += reply.prompt_tokens
        result.completion_tokens += reply.completion_tokens
        return reply.content or ""

    try:
        written = ModelDrafts.model_validate_json(complete(DRAFT_PROMPT, source, DRAFTS_SCHEMA))
    except ValidationError:
        return result

    for candidate in written.drafts[:MAX_DRAFTS]:
        question = candidate.question()
        valid = not problems(question)
        quote_found = _quote_found(candidate.quote, text)
        agrees = None
        if valid and quote_found:
            options = "\n".join(
                f"{number}. {question[key]}" for number, key in enumerate(OPTION_KEYS, start=1)
            )
            asked = f"{source}\n\nQuestion: {candidate.question_title}\n{options}"
            try:
                chosen = json.loads(complete(SECOND_PROMPT, asked, OPTION_SCHEMA)).get("option")
            except json.JSONDecodeError:
                chosen = None
            agrees = chosen == candidate.correct_option
        result.judged.append(Judged(candidate, valid, quote_found, agrees))
    return result
