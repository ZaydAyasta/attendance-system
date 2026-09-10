# syntax=docker/dockerfile:1

FROM node:24-bookworm-slim AS frontend
WORKDIR /src/frontend/attendance-web
COPY src/frontend/attendance-web/package.json src/frontend/attendance-web/package-lock.json ./
RUN npm ci
COPY src/frontend/attendance-web/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS backend-build
WORKDIR /src
COPY src/backend/Attendance.Api/Attendance.Api.csproj src/backend/Attendance.Api/
RUN dotnet restore src/backend/Attendance.Api/Attendance.Api.csproj
COPY src/backend/Attendance.Api/ src/backend/Attendance.Api/
COPY --from=frontend /src/frontend/attendance-web/dist src/frontend/attendance-web/dist
RUN dotnet publish src/backend/Attendance.Api/Attendance.Api.csproj --configuration Release --no-restore --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_EnableDiagnostics=0
COPY --from=backend-build /app/publish ./
EXPOSE 8080
ENTRYPOINT ["sh", "-c", "ASPNETCORE_URLS=http://0.0.0.0:${PORT:-8080} exec dotnet Attendance.Api.dll"]
