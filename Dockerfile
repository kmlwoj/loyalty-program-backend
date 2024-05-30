#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Development
WORKDIR /src
COPY ["lojalBackend.csproj", "."]
RUN dotnet restore "./lojalBackend.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "./lojalBackend.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Development
RUN dotnet publish "./lojalBackend.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "lojalBackend.dll"]

RUN rm -rf /app/Images
RUN rm -f /app/Logs
RUN mkdir -p /app/Images
RUN mkdir -p /app/Images/Categories
RUN mkdir -p /app/Images/Offers
RUN mkdir -p /app/Images/Organizations
RUN mkdir -p /app/Logs
