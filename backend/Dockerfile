FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["BodegaDebts.Api.csproj", "./"]
RUN dotnet restore "BodegaDebts.Api.csproj"
COPY . .
RUN dotnet build "BodegaDebts.Api.csproj" -c Release -o /app/build
RUN dotnet publish "BodegaDebts.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:5086
EXPOSE 5086
ENTRYPOINT ["dotnet", "BodegaDebts.Api.dll"]
