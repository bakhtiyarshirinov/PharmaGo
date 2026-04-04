FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY backend/PharmaGo.sln backend/
COPY backend/PharmaGo.Api/ backend/PharmaGo.Api/
COPY backend/PharmaGo.Application/ backend/PharmaGo.Application/
COPY backend/PharmaGo.Domain/ backend/PharmaGo.Domain/
COPY backend/PharmaGo.Infrastructure/ backend/PharmaGo.Infrastructure/

RUN dotnet restore backend/PharmaGo.sln
RUN dotnet publish backend/PharmaGo.Api/PharmaGo.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "PharmaGo.Api.dll"]
