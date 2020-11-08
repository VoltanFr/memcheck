# Run a migration of the DB
In the folder of the DB csproj
dotnet ef --startup-project ../MemCheck.WebUI migrations add Desc

# Updating the DB (following the add of a migration)
dotnet ef --startup-project ../MemCheck.WebUI database update

The updating acts on the database registered in the _AppSettings.json_ file of the _MemCheck.WebUI_ project.

# To get rid of the DB and start with a new one
- In SLQ Server Object Explorer, delete the DB
- Delete everything in the Migrations folder of the DB project
