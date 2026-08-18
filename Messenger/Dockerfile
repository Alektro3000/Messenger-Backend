FROM mcr.microsoft.com/dotnet/aspnet:10.0 as base
WORKDIR /app
EXPOSE 5069

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy Dependencies
COPY *.csproj ./
RUN --mount=type=cache,target=/root/.nuget/packages \ 
    dotnet restore

# Copy Source Code and Build
COPY . ./
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish -c Release -o /app/publish --no-restore

# Copy compiled code from build stage to final stage
FROM base AS final
COPY --from=build /app/publish .

# Launch 
ENTRYPOINT ["dotnet", "messenger.dll"]