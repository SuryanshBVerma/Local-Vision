#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER app
WORKDIR /app
ENV ASPNETCORE_URLS=https://+:5000
ENV ASPNETCORE_HTTPS_PORT=5000
ENV ASPNETCORE_Kestrel__Certificates__Default__Password=mypassword123
ENV ASPNETCORE_Kestrel__Certificates__Default__Path=/https/aspnetapp.pfx
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Gateway/Gateway.csproj", "Gateway/"]
RUN dotnet restore "./Gateway/Gateway.csproj"
COPY . .
WORKDIR "/src/Gateway"
RUN dotnet build "./Gateway.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Gateway.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Generate certificate in the SDK image
RUN mkdir -p /https
RUN dotnet dev-certs https -ep /https/aspnetapp.pfx -p mypassword123 --trust

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Copy certificate from build stage
USER root
COPY --from=publish /https/aspnetapp.pfx /https/aspnetapp.pfx
RUN chown app:app /https/aspnetapp.pfx
USER app

ENTRYPOINT ["dotnet", "Gateway.dll"]
