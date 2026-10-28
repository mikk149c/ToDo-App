# The web app and board server. Build from the repository root (docker-compose.yml does this):
#   docker build -f ToDo-App.Web/Dockerfile .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first, so most packages are cached until a project file changes
COPY ToDo-App/ToDo-App.csproj ToDo-App/
COPY ToDo-App.Web/ToDo-App.Web.csproj ToDo-App.Web/
RUN dotnet restore ToDo-App.Web/ToDo-App.Web.csproj

COPY ToDo-App/ ToDo-App/
COPY ToDo-App.Web/ ToDo-App.Web/
# Shared with the Windows app: the translations and the default board (linked in ToDo-App.Web.csproj)
COPY ToDo-App.UI/Properties/*.resx ToDo-App.UI/Properties/
COPY ToDo-App.UI/Assets/default_board.json ToDo-App.UI/Assets/
# Not --no-restore: the SDK only adds the package with Blazor's own script (_framework/blazor.web.js) once it sees the
# .razor files, which the restore above runs without. Restoring again here is quick, as the packages are cached
RUN dotnet publish ToDo-App.Web/ToDo-App.Web.csproj -c Release -o /app
# The Danish .resx is named da-dk, but .NET looks for da-DK, and Linux file names are case-sensitive
RUN if [ -d /app/da-dk ]; then mv /app/da-dk /app/da-DK; fi

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .

# board.json and attachments live in /data, a volume, so they survive rebuilding the container.
# The folder belongs to the image's non-root user, so a new volume is writable
RUN mkdir -p /data && chown $APP_UID /data
USER $APP_UID
ENV Board__DataDirectory=/data
VOLUME /data

# The aspnet image listens on 8080 by default
EXPOSE 8080
ENTRYPOINT ["dotnet", "ToDo-App.Web.dll"]
