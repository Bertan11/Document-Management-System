# -------- Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY DocumentManagementSystem.csproj ./
RUN dotnet restore

COPY . .
RUN dotnet publish "DocumentManagementSystem.csproj" -c Release -o /out

# -------- Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /out .
ENTRYPOINT ["dotnet", "DocumentManagementSystem.dll"]
