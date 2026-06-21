# Office Laptop Transfer Guide

Use this repository as the source of truth, but keep secrets out of source control. The project now expects sensitive values to come from environment variables or a local, untracked override.

## What to copy

Copy the repository itself, but exclude build output and local machine files:

- `bin/`
- `obj/`
- `RailwayReservationSystem.Tests/bin/`
- `RailwayReservationSystem.Tests/obj/`
- `test-output.txt`
- `.vs/`

## Required runtime values

Set these on the office laptop before running the app:

- `JWT__Secret`
- `JWT__ValidIssuer`
- `JWT__ValidAudience`
- `ConnectionStrings__DefaultConnection`
- `EmailSettings__SmtpHost`
- `EmailSettings__SmtpPort`
- `EmailSettings__SenderName`
- `EmailSettings__SenderEmail`
- `EmailSettings__Username`
- `EmailSettings__Password`
- `EmailSettings__UseSsl`
- `Stripe__SecretKey`
- `Stripe__PublishableKey`
- `Stripe__BaseUrl`

## PowerShell example

Set the values in the current session before running the app:

```powershell
$env:JWT__Secret = "<your secret>"
$env:JWT__ValidIssuer = "http://localhost:5072"
$env:JWT__ValidAudience = "http://localhost:5072"
$env:ConnectionStrings__DefaultConnection = "Server=(localdb)\\MSSQLLocalDB;Database=RailwayReservationDB;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
$env:Stripe__SecretKey = "<your stripe secret>"
$env:Stripe__PublishableKey = "<your stripe publishable key>"
$env:Stripe__BaseUrl = "http://localhost:5072"
```

If your office laptop does not have LocalDB, replace `ConnectionStrings__DefaultConnection` with the corporate SQL Server connection string approved for your environment.

## Validation

Run:

```powershell
dotnet build
```

If the build succeeds, the project is ready to run with the office-specific configuration.