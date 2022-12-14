#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.


# Stage 1 - Build
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS builder

WORKDIR /app/src
EXPOSE 5000
EXPOSE 5001
EXPOSE 5002
ENV ASPNETCORE_URLS=http://*:5000;http://*:5001;http://*:5002

# Restore
COPY *.csproj .

RUN dotnet restore -r linux-x64

# Build
COPY . .

RUN dotnet publish -c Release -o /app/publish -r linux-x64 --no-self-contained --no-restore

# Stage 2 - Publish
FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app

RUN addgroup --system --gid 101 app \
    && adduser --system --ingroup app --uid 101 app


COPY --from=builder --chown=app:app /app/publish .

USER app
    
ENTRYPOINT ["./LFA"]

