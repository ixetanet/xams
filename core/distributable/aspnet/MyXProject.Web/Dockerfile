FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["MyXProject.Web/MyXProject.Web.csproj", "MyXProject.Web/"]
COPY ["MyXProject.Services/MyXProject.Services.csproj", "MyXProject.Services/"]
COPY ["MyXProject.Data/MyXProject.Data.csproj", "MyXProject.Data/"]
COPY ["MyXProject.Common/MyXProject.Common.csproj", "MyXProject.Common/"]
RUN dotnet restore "MyXProject.Web/MyXProject.Web.csproj"
COPY . .
WORKDIR "/src/MyXProject.Web"
RUN dotnet build "MyXProject.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MyXProject.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MyXProject.Web.dll"]
