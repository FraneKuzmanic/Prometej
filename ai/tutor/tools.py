"""The three tools the model reads the material with."""

import json

from tutor.material import Material
from tutor.sections import chapter_text, find

# An Admin's own text has no length limit; one section must not fill the model's context.
MAX_SECTION_CHARACTERS = 20_000


def _tool(name: str, description: str, properties: dict) -> dict:
    return {
        "type": "function",
        "function": {
            "name": name,
            "description": description,
            "strict": True,
            "parameters": {
                "type": "object",
                "properties": properties,
                "required": list(properties),
                "additionalProperties": False,
            },
        },
    }


TOOLS = [
    _tool(
        "search_material",
        "Find where a word occurs in the material of all twelve periods. The query is matched "
        "literally inside one paragraph, ignoring case and diacritics, so give one word or a "
        "stem of it, not a phrase or a question. Returns up to three passages per period, each "
        "with the section it is in.",
        {"query": {"type": "string", "description": "One word or a stem, 2 to 100 characters"}},
    ),
    _tool(
        "list_sections",
        "List the sections (chapters and their parts) of one period's material.",
        {"period_id": {"type": "integer"}},
    ),
    _tool(
        "read_section",
        "Read the full text of one section. Reading a chapter returns its parts as well. "
        "Quotes are checked against this text.",
        {"period_id": {"type": "integer"}, "section_id": {"type": "string"}},
    ),
]


def _json(value: object) -> str:
    return json.dumps(value, ensure_ascii=False)


def dispatch(name: str, arguments: str, material: Material) -> str:
    """Runs one tool call. A mistake of the model's comes back as text it can read."""
    try:
        args = json.loads(arguments)
    except json.JSONDecodeError:
        return _json({"error": "The arguments were not valid JSON."})
    if not isinstance(args, dict):
        return _json({"error": "The arguments must be a JSON object."})

    if name == "search_material":
        hits = material.search(str(args.get("query", "")))
        if not hits:
            return _json({"hits": [], "note": "No match. Try one word, or a shorter stem."})
        return _json(
            {
                "hits": [
                    {
                        "period_id": hit.period_id,
                        "period_name": hit.period_name,
                        "section_id": hit.section_id,
                        "section_title": hit.section_title,
                        "text": hit.text,
                    }
                    for hit in hits
                ],
                "note": "A hit without a section_id: list that period's sections to find it.",
            }
        )

    if name in ("list_sections", "read_section"):
        period_id = args.get("period_id")
        sections = material.sections(period_id) if isinstance(period_id, int) else None
        if sections is None:
            return _json({"error": f"Period {period_id} does not exist or has no material."})

        if name == "list_sections":
            return _json(
                [
                    {"section_id": section.id, "level": section.level, "title": section.title}
                    for section in sections
                ]
            )

        section = find(sections, str(args.get("section_id", "")))
        if section is None:
            return _json({"error": "No such section in that period. List its sections first."})
        text = chapter_text(sections, section.id) or ""
        result: dict = {
            "period_id": period_id,
            "section_id": section.id,
            "title": section.title,
            "text": text[:MAX_SECTION_CHARACTERS],
        }
        if len(text) > MAX_SECTION_CHARACTERS:
            result["note"] = "The text was cut. Read the section's parts one by one."
        return _json(result)

    return _json({"error": f"There is no tool named {name}."})
