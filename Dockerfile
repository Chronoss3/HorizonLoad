# Stage 1: Build and publish the eventbus application
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /app
COPY . .
COPY orchestration.yaml /app/
RUN dotnet publish -c Release -o out

# Use the official .NET runtime image as the base image for the final image
FROM mcr.microsoft.com/dotnet/runtime:7.0 AS runtime


# Set the working directory inside the container
WORKDIR /app

# Copy the published output from the build image to the runtime image
COPY --from=build /app/out .
COPY orchestration.yaml /app/

# Compile and run the .NET console application upon container creation
CMD ["dotnet", "HorizonLoad.dll"]
