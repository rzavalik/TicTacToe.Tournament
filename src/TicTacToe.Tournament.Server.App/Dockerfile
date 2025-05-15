FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY TicTacToe.Tournament.sln ./
COPY Directory.Build.props ./
COPY TicTacToe.Tournament.Models/ ./TicTacToe.Tournament.Models/
COPY TicTacToe.Tournament.Auth/ ./TicTacToe.Tournament.Auth/
COPY TicTacToe.Tournament.Server/ ./TicTacToe.Tournament.Server/
COPY TicTacToe.Tournament.Server.App/ ./TicTacToe.Tournament.Server.App/

WORKDIR /src/TicTacToe.Tournament.Server.App

RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish
RUN rm -f /app/publish/appsettings.json

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish ./
ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80

ENTRYPOINT ["dotnet", "TicTacToe.Tournament.Server.App.dll"]
