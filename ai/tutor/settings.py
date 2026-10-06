from pathlib import Path

from pydantic import field_validator
from pydantic_settings import BaseSettings, SettingsConfigDict

# One .env at the repository root serves every project that needs a secret.
ROOT = Path(__file__).resolve().parents[2]


class Settings(BaseSettings):
    model_config = SettingsConfigDict(env_file=ROOT / ".env", extra="ignore")

    # Empty until the service is configured; the first question then fails, the app starts.
    azure_openai_endpoint: str = ""
    azure_openai_key: str = ""
    azure_openai_deployment: str = ""
    azure_openai_api_version: str = ""

    prometej_api_url: str = "https://localhost:7041"
    prometej_api_ca_file: str | None = None

    max_tool_calls: int = 6

    @field_validator("prometej_api_ca_file")
    @classmethod
    def _from_the_root(cls, value: str | None) -> str | None:
        """A relative path is relative to the repository, wherever the service is started."""
        return str(ROOT / value) if value else None
