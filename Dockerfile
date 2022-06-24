FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine as build
WORKDIR /app
EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000

COPY ./B2CAuthZ.Admin.WebApiHost ./B2CAuthZ.Admin.WebApiHost
COPY ./B2CAuthZ.Admin ./B2CAuthZ.Admin
# RUN ls -la
# RUN cd ./B2CAuthZ.Admin.WebApiHost
RUN dotnet restore ./B2CAuthZ.Admin.WebApiHost/B2CAuthZ.Admin.WebApiHost.csproj
RUN dotnet publish ./B2CAuthZ.Admin.WebApiHost/B2CAuthZ.Admin.WebApiHost.csproj -o /app/published-app

FROM mcr.microsoft.com/dotnet/aspnet:6.0-alpine as runtime
WORKDIR /app
COPY --from=build /app/published-app /app
EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000
ENTRYPOINT [ "dotnet", "/app/B2CAuthZ.Admin.WebApiHost.dll" ]

# https://hub.docker.com/_/microsoft-dotnet
# FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
# WORKDIR /source

# copy csproj and restore as distinct layers
# COPY *.sln .
# COPY /*.csproj ./aspnetapp/
# RUN dotnet restore

# copy everything else and build app
# COPY aspnetapp/. ./aspnetapp/
# WORKDIR /source/aspnetapp
# RUN dotnet publish -c release -o /app --no-restore

# final stage/image
#FROM mcr.microsoft.com/dotnet/aspnet:6.0
#WORKDIR /app
#COPY --from=build /app ./
#ENTRYPOINT ["dotnet", "aspnetapp.dll"]
