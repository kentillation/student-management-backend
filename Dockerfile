FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# copy csproj and restore as distinct layers
COPY *.sln .
COPY StudentManagementAPI.csproj ./
RUN dotnet restore StudentManagementAPI.csproj

# copy everything and publish
COPY . ./
RUN dotnet publish StudentManagementAPI.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./
ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80
ENTRYPOINT ["dotnet", "StudentManagementAPI.dll"]
