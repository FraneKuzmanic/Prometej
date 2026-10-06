"""The learning material, read through the public endpoints of the Prometej API."""

from dataclasses import dataclass
from typing import Protocol

import httpx

from tutor.sections import Section, clean_text, parse

MAX_QUERY_LENGTH = 100


@dataclass(frozen=True)
class Period:
    id: int
    name: str
    time_frame: str


@dataclass(frozen=True)
class Hit:
    period_id: int
    period_name: str
    section_id: str | None
    section_title: str | None
    text: str


class Material(Protocol):
    def periods(self) -> list[Period]: ...

    def sections(self, period_id: int) -> list[Section] | None:
        """None when the Period does not exist or has no content."""
        ...

    def search(self, query: str) -> list[Hit]: ...


def locate(passage: str, sections: list[Section]) -> Section | None:
    """The section a search passage was cut from."""
    text = passage.removeprefix("…").removesuffix("…").strip()
    if not text:
        return None
    for section in sections:
        if text in section.text:
            return section
    # The passage is itself a heading.
    return next((section for section in sections if section.title == text), None)


class ApiMaterial:
    def __init__(
        self,
        base_url: str,
        ca_file: str | None = None,
        transport: httpx.BaseTransport | None = None,
    ) -> None:
        self._client = httpx.Client(
            base_url=base_url, verify=ca_file or True, timeout=10, transport=transport
        )

    def periods(self) -> list[Period]:
        response = self._client.get("/api/period")
        response.raise_for_status()
        return [Period(item["id"], item["name"], item["timeFrame"]) for item in response.json()]

    def html(self, period_id: int) -> str | None:
        response = self._client.get(f"/api/period/content/{period_id}")
        if response.status_code == 404:
            return None
        response.raise_for_status()
        return response.json()["content"]

    def sections(self, period_id: int) -> list[Section] | None:
        html = self.html(period_id)
        return None if html is None else parse(html)

    def search(self, query: str) -> list[Hit]:
        query = clean_text(query)[:MAX_QUERY_LENGTH]
        response = self._client.get("/api/period/content/search", params={"query": query})
        response.raise_for_status()

        hits: list[Hit] = []
        for result in response.json():
            sections = self.sections(result["periodId"]) or []
            for passage in result["passages"]:
                section = locate(passage["text"], sections)
                hits.append(
                    Hit(
                        period_id=result["periodId"],
                        period_name=result["periodName"],
                        section_id=section.id if section else None,
                        section_title=section.title if section else None,
                        text=passage["text"],
                    )
                )
        return hits
