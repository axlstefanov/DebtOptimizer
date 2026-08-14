FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /compile
COPY *.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish DebtOptimizer.csproj -c Release -o /out --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /out .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "DebtOptimizer.dll"]