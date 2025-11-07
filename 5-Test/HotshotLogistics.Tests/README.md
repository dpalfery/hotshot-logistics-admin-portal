# HotshotLogistics.Tests

xUnit test project containing unit, integration and architecture tests.
The tests verify the boundaries defined by Clean Architecture using tools like
xUnit, Moq and NetArchTest.

## Tech Stack
- xUnit
- Moq
- NetArchTest

## Run Tests

```bash
dotnet test
```

## Environment Variables for Integration Tests

Integration tests require the following environment variables to be set for database connectivity:

- `HOTSHOT_DB_SERVER`: SQL Server instance (e.g., `localhost,1433`)
- `HOTSHOT_DB_NAME`: Database name (e.g., `HotshotLogisticsTest`)
- `HOTSHOT_DB_APP_USER`: Application user username
- `HOTSHOT_DB_PASSWORD`: Application user password

These variables are provisioned by the DbSetup CLI tool. Ensure the database is set up and running before executing integration tests.

