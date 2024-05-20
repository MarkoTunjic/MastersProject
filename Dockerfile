# Use the official .NET Core runtime as the base image
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

# Use the official .NET Core SDK as the build image
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /app

# Install the dotnet tool
RUN dotnet tool install --global ErikEJ.EFCorePowerTools.Cli --version 6.*
ENV PATH="${PATH}:/root/.dotnet/tools"

# Copy csproj and restore as distinct layers
COPY ["GrpcGenerator/GrpcGenerator.csproj", "./"]
RUN dotnet restore "GrpcGenerator.csproj"

# Copy everything else and build
COPY . .
WORKDIR /app
RUN dotnet build "GrpcGenerator/GrpcGenerator.csproj" -c Release -o /app/build

# Publish the app
FROM build AS publish
RUN dotnet publish "GrpcGenerator/GrpcGenerator.csproj" -c Release -o /app/publish

# Build the final image using the base image and the published output
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=build /root/.dotnet/tools /root/.dotnet/tools
COPY templates ../templates
ENV PATH="${PATH}:/root/.dotnet/tools"
ENTRYPOINT ["dotnet", "GrpcGenerator.dll"]
