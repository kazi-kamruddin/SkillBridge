FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY SkillBridge.Core/SkillBridge.Core.csproj SkillBridge.Core/
RUN dotnet restore SkillBridge.Core/SkillBridge.Core.csproj
COPY SkillBridge.Core/ SkillBridge.Core/
RUN dotnet publish SkillBridge.Core/SkillBridge.Core.csproj -c Release --no-restore -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://0.0.0.0:10000
ENV TZ=Asia/Dhaka
EXPOSE 10000
USER $APP_UID
ENTRYPOINT ["dotnet", "SkillBridge.Core.dll"]
