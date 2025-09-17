FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated9.0 AS base
WORKDIR /home/site/wwwroot
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["models/snipsnop.Models.csproj", "models/"]
COPY ["services/snipsnop.Services.csproj", "services/"]
COPY ["snipsnop-api-2.sln", "."]
RUN dotnet restore "snipsnop-api-2.sln"
COPY . .
RUN dotnet build "snipsnop-api-2.sln" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "services/snipsnop.Services.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /home/site/wwwroot
COPY --from=publish /app/publish .
ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true