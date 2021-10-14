#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["src/Presentation.API/Presentation.API.csproj", "src/Presentation.API/"]
RUN dotnet restore "src/Presentation.API/Presentation.API.csproj"
COPY . .
WORKDIR "/src/src/Presentation.API"
RUN dotnet build "Presentation.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Presentation.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Presentation.API.dll"]