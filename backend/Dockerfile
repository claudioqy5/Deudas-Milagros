FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["BodegaDebts.Api/BodegaDebts.Api.csproj", "BodegaDebts.Api/"]
RUN dotnet restore "BodegaDebts.Api/BodegaDebts.Api.csproj"
COPY . .
WORKDIR "/src/BodegaDebts.Api"
RUN dotnet build "BodegaDebts.Api.csproj" -c Release -o /app/build
RUN dotnet publish "BodegaDebts.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:5086
EXPOSE 5086
ENTRYPOINT ["dotnet", "BodegaDebts.Api.dll"]
