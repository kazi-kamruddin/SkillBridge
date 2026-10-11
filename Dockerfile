FROM node:22-bookworm-slim AS tailwind
WORKDIR /src
COPY package.json package-lock.json ./
RUN npm ci
COPY SkillBridge.Core/ SkillBridge.Core/
RUN npm run build:css

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY SkillBridge.Core/SkillBridge.Core.csproj SkillBridge.Core/
RUN dotnet restore SkillBridge.Core/SkillBridge.Core.csproj
COPY SkillBridge.Core/ SkillBridge.Core/
COPY --from=tailwind /src/SkillBridge.Core/wwwroot/Content/tailwind-home.css SkillBridge.Core/wwwroot/Content/tailwind-home.css
RUN dotnet publish SkillBridge.Core/SkillBridge.Core.csproj -c Release --no-restore -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://0.0.0.0:10000
ENV TZ=Asia/Dhaka
EXPOSE 10000
USER $APP_UID
ENTRYPOINT ["dotnet", "SkillBridge.Core.dll"]
