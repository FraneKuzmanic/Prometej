"""How one item of each set is scored. No model is asked: a script decides."""

from tutor.schemas import Citation
from tutor.sections import Section


def choice_right(correct_option: int, option: int | None) -> bool:
    """Set A. An answer that failed the quote check carries no option, so it is wrong."""
    return option == correct_option


def coverage_right(
    period_id: int, headings: list[str], citations: list[Citation], sections: list[Section]
) -> bool:
    """Set B: some citation is in a section the item names, in a part of one or in its chapter."""
    titles = {section.id: section.title for section in sections}
    parents = {section.id: section.parent_id for section in sections}
    wanted = set(headings)

    for citation in citations:
        if citation.period_id != period_id or citation.section_id not in titles:
            continue
        cited = citation.section_id
        parent = parents[cited]
        if titles[cited] in wanted:
            return True
        # A part of a chapter that is named.
        if parent is not None and titles[parent] in wanted:
            return True
        # The chapter of a part that is named: reading a chapter returns its parts.
        if any(parents[other] == cited and titles[other] in wanted for other in titles):
            return True
    return False


def refusal_right(kind: str) -> bool:
    """Set C: anything but an answer. "not_covered" and "declined" both refuse."""
    return kind != "answer"
