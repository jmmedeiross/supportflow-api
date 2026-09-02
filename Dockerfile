FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY SupportFlow.slnx global.json ./
COPY src/SupportFlow.Api/SupportFlow.Api.csproj src/SupportFlow.Api/
COPY tests/SupportFlow.Tests/SupportFlow.Tests.csproj tests/SupportFlow.Tests/
RUN dotnet restore SupportFlow.slnx
COPY . .
RUN dotnet publish src/SupportFlow.Api/SupportFlow.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "SupportFlow.Api.dll"]
