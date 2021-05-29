# MemCheck

This repo holds the source code for the MemCheck web site.
MemCheck is a flashcard web site, a tool to help you know things by heart.

## Usage

There is no such thing as _installing_ MemCheck on a user's machine, since it is simply a web site.
Just access the site.

## Documentation

The end user doc is available from links at the bottom of each page in MemCheck, providing contextual information.

## Development

The website is built with ASP.NET Core and Vue.js. It also uses a number of third party packages.  
Contributions are welcome, please create pull requests or issues on the [GitHub project](https://github.com/VoltanFr/memcheck).

### Prerequesites

- [.NET 5 SDK](https://dotnet.microsoft.com/download/dotnet/5.0)
- [Entity Framework .NET CLI](https://docs.microsoft.com/en-us/ef/core/cli/dotnet)
  - Run this in a console: `dotnet tool install --global dotnet-ef`
- To run the web site, you will need to trust the local issued certificate. For that, run the two following commands:
  - `dotnet dev-certs https --clean`
  - `dotnet dev-certs https -t`

### In VsCode

[Visual Studio Code](https://code.visualstudio.com/) is the way to go if you're developing on another platform than Windows (on which choosing VsCode vs Visual Studio is more subject to debate).

Recommended extensions :

- [Visual Studio IntelliCode](https://visualstudio.microsoft.com/fr/services/intellicode/)
- [C#](https://github.com/OmniSharp/omnisharp-vscode)
- [NuGet Package Manager](https://marketplace.visualstudio.com/items?itemName=jmrog.vscode-nuget-package-manager)
- [Live server], in which you need to activate https

Just open the folder in VS Code, and you're all set! For example, to run the unit tests, use the `run task` command and select `test`.

If you're new to developing with VS Code, check [this video by Tim Corey](https://www.youtube.com/watch?v=r5dtl9Uq9V0).

Note that I work with Visual Studio, so the development experience with VsCode is yours to improve! I just gave it a try and it seems ok.

### In Visual Studio

That should be easy : just open the sln file.

### Database

The production database is in [Microsoft Azure](https://portal.azure.com). Obviously, we won't allow you to use the production database for development.  
While this is not an absolute prerequesite, you will very soon need a database on your machine to be able to use MemCheck locally.

- [Microsoft SQL Server](https://docs.microsoft.com/en-us/sql/database-engine/install-windows/install-sql-server)

### Projects

- [MemCheck.Basics](https://github.com/VoltanFr/memcheck/blob/master/MemCheck.Basics/MemCheck.Basics.csproj) contains very rudimentary services, which often exist in order to save writing boilerplate code.
- [MemCheck.Domain](https://github.com/VoltanFr/memcheck/blob/master/MemCheck.Domain/MemCheck.Domain.csproj) contains the fundamental data structures which depict the register of MemCheck. Only data as persisted in the database, no behavior.
- [MemCheck.Database](https://github.com/VoltanFr/memcheck/blob/master/MemCheck.Database/MemCheck.Database.csproj) contains the [MemCheckDbContext](https://github.com/VoltanFr/memcheck/blob/master/MemCheck.Database/MemCheckDbContext.cs) class, which is sort of the entry point for the database. It also includes the [migrations](https://github.com/VoltanFr/memcheck/tree/master/MemCheck.Database/Migrations).
- [MemCheck.Application](https://github.com/VoltanFr/memcheck/blob/master/MemCheck.Application/MemCheck.Application.csproj) defines the algorithms, the way we interact with the database. It returns raw data, describing what to display, not how to display it.
- [MemCheck.WebUI](https://github.com/VoltanFr/memcheck/blob/master/MemCheck.WebUI/MemCheck.WebUI.csproj) is the ASP.NET Core project which defines the web site. It contains as little algorithm as possible, and focuses on the UI. It is the job of the controllers to adapt the data for correct display.
- [AzFunc.Notifier](https://github.com/VoltanFr/memcheck/blob/master/MemCheck.AzFunc.Notifier/MemCheck.AzFunc.Notifier.csproj) is an Azure function project to run scheduled tasks.
- [MemCheck.CommandLineDbClient](https://github.com/VoltanFr/memcheck/blob/master/MemCheck.CommandLineDbClient/MemCheck.CommandLineDbClient.csproj) is a tool to manage the database from the command line.

### Technologies

- The web site is an [ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core) project.
- The database is used through [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/) with a [code first](https://entityframeworkcore.com/approach-code-first) approach.
- The Javascript code uses [Vue.js](https://vuejs.org/).

### Third parties

- [Svg](https://www.nuget.org/packages/Svg/) for handling SVG image files.
- [Serilog](https://www.nuget.org/packages/Serilog/) for logging.
- [Markdig](https://www.nuget.org/packages/Markdig/) for MarkDown.
- [Showdown](http://showdownjs.com/) for Markdown.
- [Vue.js](https://vuejs.org/) for javascript with components.
- [SendGrid](https://www.nuget.org/packages/SendGrid/) for mailing.
- [BootstrapVue](https://bootstrap-vue.org/) for GUI and Vue.
- [Axios](https://axios-http.com/) for HTTP requests.
- [Bootstrap](https://getbootstrap.com/) for easy GUI.
- [Font Awesome](https://fontawesome.com/) for nice display.
