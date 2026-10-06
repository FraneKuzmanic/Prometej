import json
import logging
from dataclasses import asdict

import httpx
import openai
from fastapi import FastAPI, HTTPException

from tutor import agent, drafts
from tutor.material import ApiMaterial, Material
from tutor.model import AzureModel, Model
from tutor.schemas import Answer, AskRequest, Draft, Drafts, DraftsRequest
from tutor.settings import Settings

logger = logging.getLogger("tutor")


def create_app(model: Model | None = None, material: Material | None = None) -> FastAPI:
    app = FastAPI(title="Prometej tutor")
    settings = Settings()
    parts: dict = {"model": model, "material": material}

    # Built on the first question, so the app can be imported and its health asked without a key.
    def the_model() -> Model:
        if parts["model"] is None:
            parts["model"] = AzureModel(settings)
        return parts["model"]

    def the_material() -> Material:
        if parts["material"] is None:
            parts["material"] = ApiMaterial(
                settings.prometej_api_url, settings.prometej_api_ca_file
            )
        return parts["material"]

    @app.get("/health")
    def health() -> dict[str, str]:
        return {"status": "ok"}

    @app.post("/ask", response_model=Answer, response_model_by_alias=True)
    def ask(request: AskRequest) -> Answer:
        try:
            outcome = agent.ask(
                request.question,
                request.history,
                request.period_id,
                the_model(),
                the_material(),
                settings.max_tool_calls,
            )
        except (openai.OpenAIError, httpx.HTTPError) as error:
            logger.warning("ask failed: %s", type(error).__name__)
            raise HTTPException(status_code=502, detail="The model or the material failed.")

        # Numbers and names only: no question, answer or quote is ever logged.
        logger.info(json.dumps(asdict(outcome.stats)))
        return outcome.answer

    @app.post("/drafts", response_model=Drafts, response_model_by_alias=True)
    def draft(request: DraftsRequest) -> Drafts:
        try:
            drafted = drafts.draft(
                request.period_id, request.section_id, the_model(), the_material()
            )
        except drafts.UnknownSection:
            raise HTTPException(status_code=404, detail="No such section.")
        except (openai.OpenAIError, httpx.HTTPError) as error:
            logger.warning("drafts failed: %s", type(error).__name__)
            raise HTTPException(status_code=502, detail="The model or the material failed.")

        kept = drafted.kept
        logger.info(
            json.dumps(
                {
                    "drafts": len(kept),
                    "dropped": drafted.dropped,
                    "disagreed": sum(not judged.agrees for judged in kept),
                    "prompt_tokens": drafted.prompt_tokens,
                    "completion_tokens": drafted.completion_tokens,
                }
            )
        )
        return Drafts(
            drafts=[
                Draft(**judged.draft.model_dump(), agrees=bool(judged.agrees)) for judged in kept
            ],
            dropped=drafted.dropped,
        )

    return app


logging.basicConfig(level=logging.INFO, format="%(asctime)s %(name)s %(message)s")
# The HTTP clients log every address they call, the model's endpoint included.
for name in ("httpx", "httpx2", "openai"):
    logging.getLogger(name).setLevel(logging.WARNING)
app = create_app()
