FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/Showlist2026.Core/Showlist2026.Core.csproj", "Showlist2026.Core/"]
COPY ["src/Showlist2026.Data/Showlist2026.Data.csproj", "Showlist2026.Data/"]
COPY ["src/Showlist2026.Web/Showlist2026.Web.csproj", "Showlist2026.Web/"]
RUN dotnet restore "Showlist2026.Web/Showlist2026.Web.csproj"
COPY src/ .
RUN dotnet publish "Showlist2026.Web/Showlist2026.Web.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Showlist2026.Web.dll"]
