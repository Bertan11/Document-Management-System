FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["DocumentManagementSystem.csproj", "."]
RUN dotnet restore "DocumentManagementSystem.csproj"
COPY . .
WORKDIR "/src"
RUN dotnet build "DocumentManagementSystem.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DocumentManagementSystem.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DocumentManagementSystem.dll"]