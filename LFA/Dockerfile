#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["LFA/LFA.csproj", "LFA/"]
RUN dotnet restore "LFA/LFA.csproj"
COPY . .
WORKDIR "/src/LFA"
RUN dotnet build "LFA.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LFA.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app

RUN addgroup --system --gid 101 app \
	& adduser --system --ingroup app --uid 101 app

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LFA.dll"]