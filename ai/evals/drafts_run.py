"""Asks for question drafts from one section of each Period and records every draft written.

    python -m evals.drafts_run

The section is the first work under "Djela". Nothing is dropped silently here: a draft the
app would refuse, or whose quote is not in the section, is recorded as such.
"""

import json
import sys
from datetime import datetime

from evals.run import RESULTS
from tutor import drafts
from tutor.material import ApiMaterial
from tutor.model import AzureModel
from tutor.sections import Section
from tutor.settings import Settings

WORKS = "Djela"


def first_work(sections: list[Section]) -> Section | None:
    chapter = next((s for s in sections if s.level == 2 and s.title == WORKS), None)
    return next((s for s in sections if chapter and s.parent_id == chapter.id), None)


def share(part: int, whole: int) -> float | None:
    return round(part / whole, 4) if whole else None


def summary(items: list[dict], prompt_tokens: int, completion_tokens: int) -> dict:
    kept = [item for item in items if item["valid"] and item["quoteFound"]]
    valid = sum(item["valid"] for item in items)
    found = sum(item["quoteFound"] for item in items)
    agreed = sum(bool(item["agrees"]) for item in kept)
    return {
        "written": len(items),
        "valid": valid,
        "valid_share": share(valid, len(items)),
        "quote_found": found,
        "quote_found_share": share(found, len(items)),
        "kept": len(kept),
        "agrees": agreed,
        "agrees_share": share(agreed, len(kept)),
        "prompt_tokens": prompt_tokens,
        "completion_tokens": completion_tokens,
    }


def main() -> None:
    sys.stdout.reconfigure(encoding="utf-8")
    settings = Settings()
    model = AzureModel(settings)
    material = ApiMaterial(settings.prometej_api_url, settings.prometej_api_ca_file)

    items: list[dict] = []
    prompt_tokens = completion_tokens = 0
    for period in sorted(material.periods(), key=lambda period: period.id):
        section = first_work(material.sections(period.id) or [])
        if section is None:
            print(f"{period.name}: no work under {WORKS}")
            continue
        drafted = drafts.draft(period.id, section.id, model, material)
        prompt_tokens += drafted.prompt_tokens
        completion_tokens += drafted.completion_tokens
        for number, judged in enumerate(drafted.judged, start=1):
            items.append(
                {
                    "id": f"p{period.id}-{number}",
                    "periodId": period.id,
                    "periodName": period.name,
                    "sectionId": section.id,
                    "sectionTitle": section.title,
                    **judged.draft.question(),
                    "quote": judged.draft.quote,
                    "valid": judged.valid,
                    "quoteFound": judged.quote_found,
                    "agrees": judged.agrees,
                }
            )
        print(f"{period.name}: {len(drafted.judged)} written, {len(drafted.kept)} kept")

    totals = summary(items, prompt_tokens, completion_tokens)
    record = {
        "set": "drafts",
        "date": datetime.now().astimezone().date().isoformat(),
        "deployment": settings.azure_openai_deployment,
        "summary": totals,
        "items": items,
    }
    RESULTS.mkdir(exist_ok=True)
    file = RESULTS / f"{record['date']}-{settings.azure_openai_deployment}-drafts.json"
    if file.exists():
        sys.exit(f"{file.name} exists. A recorded run is kept, not overwritten.")
    file.write_text(json.dumps(record, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(totals))


if __name__ == "__main__":
    main()
