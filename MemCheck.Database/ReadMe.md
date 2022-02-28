# Run a migration of the DB
In the folder of the DB csproj
dotnet ef --startup-project ../MemCheck.WebUI migrations add IntroduceImageSha1

# Updating the DB (following the add of a migration)
dotnet ef --startup-project ../MemCheck.WebUI database update

The updating acts on the database registered in the _AppSettings.json_ file of the _MemCheck.WebUI_ project.

# To get rid of the DB and start with a new one
- In SLQ Server Object Explorer, delete the DB
- Delete everything in the Migrations folder of the DB project

# To update the tooling
dotnet tool update --global dotnet-ef

# To uninstall the tooling
dotnet tool uninstall --global dotnet-ef

# To install a specific version of the tooling
dotnet tool install dotnet-ef --global --version 5.0.10
