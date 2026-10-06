from dataclasses import dataclass, field
from typing import Protocol

from openai import AzureOpenAI

from tutor.settings import Settings


@dataclass(frozen=True)
class ToolCall:
    id: str
    name: str
    arguments: str


@dataclass(frozen=True)
class ModelReply:
    content: str | None
    tool_calls: list[ToolCall] = field(default_factory=list)
    prompt_tokens: int = 0
    completion_tokens: int = 0


class Model(Protocol):
    def complete(
        self, messages: list[dict], tools: list[dict], schema: dict, allow_tools: bool
    ) -> ModelReply: ...


class AzureModel:
    """GPT-4.1 on Azure OpenAI. The deployment's name is configuration."""

    def __init__(self, settings: Settings) -> None:
        self._client = AzureOpenAI(
            azure_endpoint=settings.azure_openai_endpoint,
            api_key=settings.azure_openai_key,
            api_version=settings.azure_openai_api_version,
            max_retries=2,
        )
        self._deployment = settings.azure_openai_deployment

    def complete(
        self, messages: list[dict], tools: list[dict], schema: dict, allow_tools: bool
    ) -> ModelReply:
        extra: dict = {}
        if tools:
            # A strict schema is not supported together with parallel tool calls.
            extra = {
                "tools": tools,
                "parallel_tool_calls": False,
                "tool_choice": "auto" if allow_tools else "none",
            }
        response = self._client.chat.completions.create(
            model=self._deployment,
            messages=messages,
            response_format={"type": "json_schema", "json_schema": schema},
            temperature=0,
            timeout=60,
            **extra,
        )
        message = response.choices[0].message
        usage = response.usage
        return ModelReply(
            content=message.content,
            tool_calls=[
                ToolCall(call.id, call.function.name, call.function.arguments)
                for call in message.tool_calls or []
            ],
            prompt_tokens=usage.prompt_tokens if usage else 0,
            completion_tokens=usage.completion_tokens if usage else 0,
        )
