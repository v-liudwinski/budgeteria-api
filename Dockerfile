FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/Budgeteria.Api/Budgeteria.Api.csproj src/Budgeteria.Api/
RUN dotnet restore src/Budgeteria.Api/Budgeteria.Api.csproj
COPY . .
RUN dotnet publish src/Budgeteria.Api/Budgeteria.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "Budgeteria.Api.dll"]
