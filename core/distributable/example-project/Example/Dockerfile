FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["Example.Web/Example.Web.csproj", "Example.Web/"]
COPY ["Example.Services/Example.Services.csproj", "Example.Services/"]
COPY ["Example.Data/Example.Data.csproj", "Example.Data/"]
COPY ["Example.Common/Example.Common.csproj", "Example.Common/"]
RUN dotnet restore "Example.Web/Example.Web.csproj"
COPY . .
WORKDIR "/src/Example.Web"
RUN dotnet build "Example.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Example.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Example.Web.dll"]
