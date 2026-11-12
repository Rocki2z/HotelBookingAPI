# Build from the repository root: docker build -f HotelBookingAPI/Dockerfile .
FROM mcr.microsoft.com/dotnet/sdk:10.0.301 AS build
WORKDIR /src

COPY HotelBookingAPI/HotelBookingAPI.csproj HotelBookingAPI/
RUN dotnet restore HotelBookingAPI/HotelBookingAPI.csproj

COPY HotelBookingAPI/ HotelBookingAPI/
RUN dotnet publish HotelBookingAPI/HotelBookingAPI.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "HotelBookingAPI.dll"]
