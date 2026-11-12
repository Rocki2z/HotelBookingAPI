# Hotel Booking API tests

The test project uses xUnit. Fast tests cover cancellation policy, DTO validation, and the mock payment gateway. PostgreSQL integration tests exercise the real EF Core/Npgsql service paths and the HTTP pipeline, including registration, login, authorization, booking conflicts, and payment/refund state changes.

Run the fast suite from the repository folder:

```powershell
dotnet test .\HotelBookingAPI.Tests\HotelBookingAPI.Tests.csproj
```

PostgreSQL integration tests require Docker Desktop (or another working Docker daemon). Enable them in PowerShell with:

```powershell
$env:HOTELBOOKING_RUN_POSTGRES_TESTS = 'true'
dotnet test .\HotelBookingAPI.Tests\HotelBookingAPI.Tests.csproj
Remove-Item Env:HOTELBOOKING_RUN_POSTGRES_TESTS
```

The fixture starts an isolated `postgres:16-alpine` container and creates a disposable database. It does not use the application's configured development database.

Collect Cobertura coverage while running tests:

```powershell
dotnet test .\HotelBookingAPI.Tests\HotelBookingAPI.Tests.csproj --collect:"XPlat Code Coverage"
```

Coverage XML files are written under `HotelBookingAPI.Tests\TestResults\<run-id>\coverage.cobertura.xml`.
