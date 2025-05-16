FROM node:18 AS build-typescript
WORKDIR /webapp

COPY src/TicTacToe.Tournament.WebApp/package.json ./package.json
COPY src/TicTacToe.Tournament.WebApp/tsconfig.json ./tsconfig.json
COPY src/TicTacToe.Tournament.WebApp/wwwroot ./wwwroot
RUN npm install
RUN npx tsc

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/TicTacToe.Tournament.sln ./
COPY Directory.Build.props ./
COPY src/TicTacToe.Tournament.Models/ ./TicTacToe.Tournament.Models/
COPY src/TicTacToe.Tournament.Auth/ ./TicTacToe.Tournament.Auth/
COPY src/TicTacToe.Tournament.Server/ ./TicTacToe.Tournament.Server/
COPY src/TicTacToe.Tournament.WebApp/ ./TicTacToe.Tournament.WebApp/

WORKDIR /src/TicTacToe.Tournament.WebApp

COPY --from=build-typescript /webapp/wwwroot/js/dist ./wwwroot/js/dist

RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish
RUN rm -f /app/publish/appsettings.json

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish ./
ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80

ENTRYPOINT ["dotnet", "TicTacToe.Tournament.WebApp.dll"]
