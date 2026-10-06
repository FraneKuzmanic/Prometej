"""A Period Content as sections: the text under each h2 and h3.

Two other programs read the same HTML and this file has to agree with both. The Period page
gives every h2 and h3 the id `odjeljak-{index}` (frontend, ContentsList/headings.ts), and a
citation's section id is that address. The search of the API reads the text of the innermost
blocks, cleaned (PeriodService.TextOf), and a search passage is found again in a section's text.
"""

import unicodedata
from dataclasses import dataclass

from bs4 import BeautifulSoup, Comment, NavigableString, Tag

SECTION_TAGS = {"h2", "h3"}
BLOCK_TAGS = {"h1", "h2", "h3", "h4", "h5", "h6", "p", "li", "blockquote", "pre"}


@dataclass(frozen=True)
class Section:
    id: str
    level: int
    title: str
    parent_id: str | None
    text: str


def clean_text(text: str) -> str:
    """Composed characters and single spaces."""
    return " ".join(unicodedata.normalize("NFC", text).split())


def _text_of(block: Tag) -> str:
    parts: list[str] = []
    for node in block.descendants:
        if isinstance(node, Tag):
            if node.name == "br":
                parts.append(" ")
        elif isinstance(node, NavigableString) and not isinstance(node, Comment):
            parts.append(str(node))
    return clean_text("".join(parts))


def _is_innermost_block(tag: Tag) -> bool:
    return tag.name in BLOCK_TAGS and tag.find(list(BLOCK_TAGS)) is None


def parse(html: str) -> list[Section]:
    soup = BeautifulSoup(html, "html.parser")

    sections: list[Section] = []
    lines: list[str] = []
    current: dict | None = None
    chapter_id: str | None = None

    def close() -> None:
        if current is not None:
            sections.append(Section(text="\n".join(lines), **current))

    index = -1
    for tag in soup.find_all(True):
        if tag.name in SECTION_TAGS:
            # An empty heading is counted and gets no id, as on the Period page.
            index += 1
            title = _text_of(tag)
            if title == "":
                continue
            close()
            lines = []
            level = int(tag.name[1])
            section_id = f"odjeljak-{index}"
            if level == 2:
                chapter_id = section_id
            current = {
                "id": section_id,
                "level": level,
                "title": title,
                "parent_id": chapter_id if level == 3 else None,
            }
        elif current is not None and _is_innermost_block(tag):
            text = _text_of(tag)
            if text:
                lines.append(text)
    close()
    return sections


def find(sections: list[Section], section_id: str) -> Section | None:
    return next((section for section in sections if section.id == section_id), None)


def chapter_text(sections: list[Section], section_id: str) -> str | None:
    """What reading a section returns: an h3 alone, an h2 with every part under it."""
    section = find(sections, section_id)
    if section is None:
        return None
    if section.level == 3:
        return section.text

    parts = [section.text] if section.text else []
    for part in sections:
        if part.parent_id == section.id:
            parts.append(part.title)
            if part.text:
                parts.append(part.text)
    return "\n".join(parts)
