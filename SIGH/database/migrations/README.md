# SIGH - Database Migrations

As migrations do Entity Framework Core serão armazenadas neste repositório.

Para criar uma nova migration no projeto `SIGH.Persistence`:

```bash
dotnet ef migrations add <NomeDaMigration> --project ../backend/src/SIGH.Persistence --startup-project ../backend/src/SIGH.Api
```

Para aplicar as migrations no SQL Server:

```bash
dotnet ef database update --project ../backend/src/SIGH.Persistence --startup-project ../backend/src/SIGH.Api
```
