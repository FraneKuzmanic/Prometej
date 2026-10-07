# One image for the whole app. The API serves the built client from wwwroot, so the browser
# sees one origin, which the session cookie needs. The tutor runs beside the API in the same
# container and listens on localhost only: nothing but the API can reach it.

FROM node:20-alpine AS client
WORKDIR /src
COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci
COPY frontend/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS api
WORKDIR /src
COPY backend/Prometej_api/Prometej_api.csproj Prometej_api/
COPY backend/Prometej_core/Prometej_core.csproj Prometej_core/
COPY backend/Prometej_persistance/Prometej_persistance.csproj Prometej_persistance/
RUN dotnet restore Prometej_api/Prometej_api.csproj
COPY backend/Prometej_api/ Prometej_api/
COPY backend/Prometej_core/ Prometej_core/
COPY backend/Prometej_persistance/ Prometej_persistance/
RUN dotnet publish Prometej_api/Prometej_api.csproj -c Release -o /app --no-restore

# Python's image with the ASP.NET runtime copied in: both are Debian 12.
FROM python:3.13-slim-bookworm
RUN apt-get update \
    && apt-get install -y --no-install-recommends libicu72 libstdc++6 \
    && rm -rf /var/lib/apt/lists/*
COPY --from=mcr.microsoft.com/dotnet/aspnet:8.0 /usr/share/dotnet /usr/share/dotnet
RUN ln -s /usr/share/dotnet/dotnet /usr/bin/dotnet

COPY ai/ /tutor/
RUN pip install --no-cache-dir /tutor && rm -rf /tutor

WORKDIR /app
COPY --from=api /app ./
COPY --from=client /src/dist ./wwwroot
COPY deploy/start.sh ./start.sh
CMD ["sh", "./start.sh"]
