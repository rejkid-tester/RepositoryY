# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# Base image for running and for EF tooling inside the container
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS base
WORKDIR /app
EXPOSE 5002

RUN pwd 
# Install EF Core tooling
RUN dotnet tool install --global dotnet-ef
ENV PATH="$PATH:/root/.dotnet/tools"


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
# Copy project file(s) first to leverage Docker layer caching
COPY ["BackEnd/BackEnd.csproj", "BackEnd/"]
RUN dotnet restore "./BackEnd/BackEnd.csproj"

# Copy the rest of the project files
COPY ["BackEnd/", "BackEnd/"]
WORKDIR "/src/BackEnd"
RUN dotnet build "./BackEnd.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./BackEnd.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=build /src/BackEnd ./src/BackEnd
ENTRYPOINT ["dotnet", "BackEnd.dll"]