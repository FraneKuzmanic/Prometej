# One image for the whole app: the API serves the built client from wwwroot, so the browser
# sees one origin, which the session cookie needs. The tutor (ai/) is not part of it.

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

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=api /app ./
COPY --from=client /src/dist ./wwwroot
# The host says which port to listen on (Render sets PORT); 8080 without one.
CMD ["sh", "-c", "ASPNETCORE_HTTP_PORTS=${PORT:-8080} exec dotnet Prometej_api.dll"]
