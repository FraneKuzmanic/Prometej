"""Runs one eval set against the real model and the running Prometej API.

    python -m evals.run --set choice|choice-baseline|coverage|refusal|dev [--limit N]

Every run writes one file under results/. A recorded run is not repeated for a better number.
"""

import argparse
import json
import sys
import time
from dataclasses import asdict
from datetime import datetime
from pathlib import Path

from evals.score import choice_right, coverage_right, refusal_right
from tutor import agent
from tutor.material import ApiMaterial
from tutor.model import AzureModel, Model
from tutor.settings import ROOT, Settings

HERE = Path(__file__).resolve().parent
SETS = HERE / "sets"
RESULTS = HERE / "results"
SEED = ROOT / "backend/Prometej_api/Seed/Content"

OPTION_KEYS = ["firstAnswer", "secondAnswer", "thirdAnswer", "fourthAnswer"]

BASELINE_PROMPT = (
    "You answer a multiple-choice question about literature for a Croatian high-school "
    'student. Put the number of the right option in "option".'
)
BASELINE_SCHEMA = {
    "name": "option",
    "strict": True,
    "schema": {
        "type": "object",
        "properties": {"option": {"type": "integer", "enum": [1, 2, 3, 4]}},
        "required": ["option"],
        "additionalProperties": False,
    },
}


def rows(name: str) -> list[dict]:
    lines = (SETS / name).read_text(encoding="utf-8").splitlines()
    return [json.loads(line) for line in lines if line.strip()]


def choice_items() -> list[dict]:
    """Set A: the demo quizzes' choice Questions that are not asked about a Source Text."""
    items = []
    for file in sorted((SEED / "quizzes").glob("*.json")):
        quiz = json.loads(file.read_text(encoding="utf-8"))
        for number, question in enumerate(quiz["questions"], start=1):
            if question.get("type", "choice") != "choice" or question.get("sourceTextNo"):
                continue
            options = "\n".join(
                f"{n}. {question[key]}" for n, key in enumerate(OPTION_KEYS, start=1)
            )
            items.append(
                {
                    "id": f"{file.stem}-{number}",
                    "question": f"{question['questionTitle']}\n{options}",
                    "periodId": quiz["quiz"]["periodId"],
                    "correctOption": question["correctOption"],
                }
            )
    return items


def differing_periods(material: ApiMaterial) -> list[int]:
    """The Periods whose stored text is not the committed seed file: the seeder never
    overwrites, so a local database can hold an older text."""

    def squeezed(text: str) -> str:
        return "".join(text.split())

    differing = []
    for file in (SEED / "periods").glob("*.html"):
        period_id = int(file.name.split("-")[0])
        stored = material.html(period_id)
        if stored is None or squeezed(stored) != squeezed(file.read_text(encoding="utf-8")):
            differing.append(period_id)
    return sorted(differing)


def run_item(name: str, item: dict, model: Model, material: ApiMaterial, max_tool_calls: int):
    if name == "choice-baseline":
        started = time.perf_counter()
        reply = model.complete(
            [
                {"role": "system", "content": BASELINE_PROMPT},
                {"role": "user", "content": item["question"]},
            ],
            [],
            BASELINE_SCHEMA,
            "none",
        )
        option = json.loads(reply.content or "{}").get("option")
        return {
            "expected": item["correctOption"],
            "option": option,
            "right": choice_right(item["correctOption"], option),
            "latency_ms": round((time.perf_counter() - started) * 1000),
            "prompt_tokens": reply.prompt_tokens,
            "completion_tokens": reply.completion_tokens,
        }

    outcome = agent.ask(
        item["question"],
        [],
        item.get("periodId") if name in ("choice", "dev") else None,
        model,
        material,
        max_tool_calls,
        with_option=name == "choice",
    )
    answer = outcome.answer
    result = {
        "kind": answer.kind,
        "answer": answer.answer,
        "cited": [
            {"periodId": c.period_id, "sectionId": c.section_id, "sectionTitle": c.section_title}
            for c in answer.citations
        ],
        **{key: value for key, value in asdict(outcome.stats).items() if key != "kind"},
    }
    if name == "choice":
        result |= {
            "expected": item["correctOption"],
            "option": outcome.option,
            "right": choice_right(item["correctOption"], outcome.option),
        }
    elif name == "coverage":
        sections = material.sections(item["periodId"]) or []
        result |= {
            "expected": {"periodId": item["periodId"], "headings": item["headings"]},
            "right": coverage_right(item["periodId"], item["headings"], answer.citations, sections),
        }
    elif name == "refusal":
        result |= {"expected": item["expect"], "right": refusal_right(answer.kind)}
    return result


def summary(results: list[dict]) -> dict:
    scored = [result for result in results if "right" in result]
    count = len(results)
    return {
        "items": count,
        "right": sum(result["right"] for result in scored),
        "share": round(sum(r["right"] for r in scored) / len(scored), 4) if scored else None,
        "retried": sum(bool(result.get("retried")) for result in results),
        "unverified": sum(result.get("kind") == "unverified" for result in results),
        "prompt_tokens": sum(result["prompt_tokens"] for result in results),
        "completion_tokens": sum(result["completion_tokens"] for result in results),
    }


def main() -> None:
    # The Windows console is not UTF-8 by default and the questions are Croatian.
    sys.stdout.reconfigure(encoding="utf-8")
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "--set",
        dest="name",
        required=True,
        choices=["choice", "choice-baseline", "coverage", "refusal", "dev"],
    )
    parser.add_argument("--limit", type=int)
    args = parser.parse_args()

    settings = Settings()
    model = AzureModel(settings)
    material = ApiMaterial(settings.prometej_api_url, settings.prometej_api_ca_file)

    items = (choice_items() if args.name.startswith("choice") else rows(f"{args.name}.jsonl"))[
        : args.limit
    ]
    differing = differing_periods(material)
    if differing:
        print(f"Stored text differs from the seed file for periods: {differing}")

    results = []
    for item in items:
        result = {"id": item["id"], "question": item["question"]}
        result |= run_item(args.name, item, model, material, settings.max_tool_calls)
        results.append(result)
        mark = {True: "right", False: "WRONG"}.get(result.get("right"), "-")
        print(f"{item['id']:>14}  {mark:5}  {result.get('kind', '')}")

    totals = summary(results)
    record = {
        "set": args.name,
        "date": datetime.now().astimezone().date().isoformat(),
        "deployment": settings.azure_openai_deployment,
        "limit": args.limit,
        "differs_from_seed": differing,
        "summary": totals,
        "items": results,
    }
    RESULTS.mkdir(exist_ok=True)
    file = RESULTS / f"{record['date']}-{settings.azure_openai_deployment}-{args.name}.json"
    if file.exists():
        sys.exit(f"{file.name} exists. A recorded run is kept, not overwritten.")
    file.write_text(json.dumps(record, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(totals))
    print(f"Written: {file.relative_to(HERE)}")


if __name__ == "__main__":
    main()
