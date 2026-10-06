from typing import Literal

from pydantic import BaseModel, ConfigDict, Field
from pydantic.alias_generators import to_camel

ModelKind = Literal["answer", "not_covered", "declined"]
# "unverified" is the service's own: the model's quotes failed the check twice.
Kind = Literal["answer", "not_covered", "declined", "unverified"]


class ModelCitation(BaseModel):
    period_id: int
    section_id: str
    quote: str


class ModelAnswer(BaseModel):
    """What the model has to return."""

    kind: ModelKind
    answer: str
    citations: list[ModelCitation]
    # Only the eval asks for it: which of four options is right.
    option: int | None = None


def answer_schema(with_option: bool = False) -> dict:
    """The strict JSON schema of ModelAnswer: every property required, nothing else allowed."""
    properties: dict = {
        "kind": {"type": "string", "enum": ["answer", "not_covered", "declined"]},
        "answer": {"type": "string"},
        "citations": {
            "type": "array",
            "items": {
                "type": "object",
                "properties": {
                    "period_id": {"type": "integer"},
                    "section_id": {"type": "string"},
                    "quote": {"type": "string"},
                },
                "required": ["period_id", "section_id", "quote"],
                "additionalProperties": False,
            },
        },
    }
    if with_option:
        properties["option"] = {"type": "integer", "enum": [1, 2, 3, 4]}
    return {
        "name": "tutor_answer",
        "strict": True,
        "schema": {
            "type": "object",
            "properties": properties,
            "required": list(properties),
            "additionalProperties": False,
        },
    }


class _Wire(BaseModel):
    model_config = ConfigDict(alias_generator=to_camel, populate_by_name=True)


class Turn(_Wire):
    role: Literal["user", "assistant"]
    content: str


class AskRequest(_Wire):
    question: str = Field(min_length=1)
    history: list[Turn] = []
    period_id: int | None = None


class Citation(_Wire):
    period_id: int
    period_name: str
    section_id: str
    section_title: str
    quote: str


class Answer(_Wire):
    kind: Kind
    answer: str
    citations: list[Citation]


class DraftsRequest(_Wire):
    period_id: int
    section_id: str


class Draft(_Wire):
    question_title: str
    first_answer: str
    second_answer: str
    third_answer: str
    fourth_answer: str
    correct_option: int
    # The sentence of the section that makes the correct option right.
    quote: str
    # False when a second reading of the text did not pick the correct option.
    agrees: bool


class Drafts(_Wire):
    drafts: list[Draft]
    # How many the model wrote that are not offered: invalid, or their quote was not found.
    dropped: int
