"""The check an answer passes before anyone sees it: every quote is in the text it names.

What is checked is the quote. The sentences the model writes around it are not.
"""

import unicodedata

from tutor.material import Material
from tutor.schemas import ModelAnswer
from tutor.sections import chapter_text

MIN_QUOTE = 15
MAX_QUOTE = 400


def normalise(text: str) -> str:
    """Composed characters and every run of whitespace as one space. Nothing else is forgiven:
    case, letters, quotation marks and dashes have to be the text's own."""
    return " ".join(unicodedata.normalize("NFC", text).split())


def check(answer: ModelAnswer, material: Material) -> list[str]:
    """The reasons the answer cannot be shown; empty when it can."""
    failures: list[str] = []
    if answer.kind == "answer" and not answer.citations:
        failures.append("An answer of kind 'answer' needs at least one citation.")

    for number, citation in enumerate(answer.citations, start=1):
        where = f"Citation {number} (period {citation.period_id}, {citation.section_id})"
        sections = material.sections(citation.period_id)
        if sections is None:
            failures.append(f"{where}: that period has no material.")
            continue
        text = chapter_text(sections, citation.section_id)
        if text is None:
            failures.append(f"{where}: that section does not exist in that period.")
            continue
        quote = normalise(citation.quote)
        if not MIN_QUOTE <= len(quote) <= MAX_QUOTE:
            failures.append(f"{where}: a quote is {MIN_QUOTE} to {MAX_QUOTE} characters long.")
        elif quote not in normalise(text):
            failures.append(f"{where}: the quote was not found in that section.")
    return failures
