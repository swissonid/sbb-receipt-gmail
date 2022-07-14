FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["receipt-gmail.csproj", "./"]
RUN dotnet restore "receipt-gmail.csproj"
COPY . .
WORKDIR "/src/"
RUN dotnet build "receipt-gmail.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "receipt-gmail.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "receipt-gmail.dll"]
