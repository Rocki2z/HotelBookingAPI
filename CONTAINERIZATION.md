# Hotel Booking API containers

## Services and network

`docker-compose.yml` starts two services on the private `hotelbooking` bridge network. The API connects to PostgreSQL using the service hostname `db` and port `5432`; inside a container, `localhost` refers to that same container. Only the API's HTTP port and PostgreSQL's development port are published to the host. The database data persists in the `postgres_data` named volume.

The API waits for PostgreSQL's `pg_isready` health check before starting. It applies EF Core migrations and then seeds the identity roles and configured admin account. The API listens on container port `8080`.

## Create local configuration and secrets (PowerShell)

Run these commands from the repository root. Keep `.env` and the `secrets` directory private; both are excluded from Git and the Docker build context.

```powershell
Copy-Item .env.example .env
New-Item -ItemType Directory -Force .\secrets | Out-Null

$rng = [Security.Cryptography.RandomNumberGenerator]::Create()

$dbPasswordBytes = New-Object byte[] 32
$rng.GetBytes($dbPasswordBytes)
$dbPassword = ([BitConverter]::ToString($dbPasswordBytes)).Replace('-', '').ToLowerInvariant()
Set-Content .\secrets\postgres_password $dbPassword -NoNewline

$jwtBytes = New-Object byte[] 64
$rng.GetBytes($jwtBytes)
$jwtKey = ([BitConverter]::ToString($jwtBytes)).Replace('-', '').ToLowerInvariant()
Set-Content .\secrets\jwt_signing_key $jwtKey -NoNewline

$adminBytes = New-Object byte[] 32
$rng.GetBytes($adminBytes)
$adminPassword = 'Adm1n!' + ([BitConverter]::ToString($adminBytes)).Replace('-', '').ToLowerInvariant()
Set-Content .\secrets\admin_seed_password $adminPassword -NoNewline

$rng.Dispose()
```

The admin password is generated locally and is not printed. Retrieve it when needed with `Get-Content .\secrets\admin_seed_password` and avoid pasting it into source files or chat. Change `ADMIN_SEED_EMAIL` in `.env` to the account you want seeded.

The API reads mounted files from `/run/secrets`. Secret filenames use ASP.NET configuration key syntax with double underscores, such as `Jwt__Key`. The PostgreSQL container reads its password with `POSTGRES_PASSWORD_FILE`; the API receives the same value as the `Postgres__Password` secret and builds its connection string without putting the password in an environment variable.

## Build and run

```powershell
docker compose up --build -d
docker compose ps
docker compose logs -f api
```

Open `http://localhost:8080/scalar/v1` for Scalar and `http://localhost:8080/openapi/v1.json` for the OpenAPI document when `ASPNETCORE_ENVIRONMENT=Development`. Use `POSTGRES_HOST_PORT` in `.env` to change the host-only PostgreSQL port (default `5433`). The application uses `db:5432` on its private network regardless of that host port.

For a production deployment, set `ASPNETCORE_ENVIRONMENT=Production`, provision the secret files using the deployment platform's secret manager, and terminate TLS at a trusted reverse proxy. Do not commit real secret files or expose PostgreSQL publicly.

Stop the services while retaining the database volume with `docker compose down`. To also delete the database contents, use `docker compose down --volumes`.

## Local development outside Docker

`appsettings.json` contains no database password or JWT signing key. Configure these with .NET User Secrets or environment variables. For example, from `HotelBookingAPI`:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=HotelBookingDb;Username=postgres;Password=<your-local-password>"
dotnet user-secrets set "Jwt:Key" "<a-random-secret-at-least-32-bytes>"
dotnet user-secrets set "AdminSeed:Email" "admin@example.test"
dotnet user-secrets set "AdminSeed:Password" "<a-strong-admin-password>"
```

The default local configuration does not automatically apply migrations. Run `dotnet ef database update` for local development, or set `Database:ApplyMigrationsOnStartup=true` if you want the app to migrate the configured database at startup.
