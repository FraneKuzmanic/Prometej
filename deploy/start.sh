#!/bin/sh
# What the container runs: the tutor, when it has a model to ask, and then the API.

# The host says which port to listen on (Render sets PORT); 8080 without one.
PORT="${PORT:-8080}"

if [ -n "$AZURE_OPENAI_KEY" ]; then
  # The tutor reads the material from the API in this same container, and listens on
  # localhost only. If it stops, the API says the tutor is unavailable and the app goes on.
  PROMETEJ_API_URL="http://127.0.0.1:$PORT" \
    python -m uvicorn tutor.app:app --host 127.0.0.1 --port 8000 &
  export Tutor__BaseUrl="http://127.0.0.1:8000"
fi

ASPNETCORE_HTTP_PORTS="$PORT" exec dotnet Prometej_api.dll
