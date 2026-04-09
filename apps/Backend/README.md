# Relationship Planner API

This is the ASP.NET Core backend for the relationship planner.

## Run it

```bash
dotnet run
```

It listens locally on the URLs from `launchSettings.json` and connects to the SQL Server database from the `PlannerDatabase` connection string.

For local development, the repo ships with a `ConnectionStrings:PlannerDatabase` value in `appsettings.Development.json` that targets `LocalDB`.

## What it is for

- Shared users and settings
- Shared planner entries
- Finance items with monthly budget, actual spend, and debt tracking fields, including debt remaining from total debt, initial paid, and this month's actual payment.
- The backend foundation for the Blazor web app and Android app
