# MemCheck

This repo holds the source code for the [MemCheck web site](https://memcheckfr.azurewebsites.net/).
MemCheck is a flashcard web site, a tool to help you know things by heart.
The [end user doc](https://memcheckfr.azurewebsites.net/Doc/MdRenderer?refererRoute=%2F&cultureName=fr) is available from links at the bottom of each page in MemCheck, providing contextual information.

## Development

### Technologies

- The web site is an [ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core) project.
- The database is used through [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/) with a [code first](https://entityframeworkcore.com/approach-code-first) approach.
- The Javascript code uses [Vue.js](https://vuejs.org/).

### Prerequesites on Windows

- Your dev environment, probably one of:
  - [Visual Studio Code](https://code.visualstudio.com/). Needed extension: [C#](https://github.com/OmniSharp/omnisharp-vscode).
  - [Visual Studio](https://visualstudio.microsoft.com/en/vs/), my recommendation (the free Community edition is ok). Needed workload: `ASP.NET and web development` (with at least the default components).
- A database server. I recommend [SQL Server Express](https://docs.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb) for simplicity (the basic setup is ok). After installation, run `SqlLocalDB.msi` (it should be in `C:\SQL2019\ExpressAdv_ENU\1033_ENU_LP\x64\Setup\x64`). **Already included if you installed Visual Studio**.
- [.NET 5 SDK](https://dotnet.microsoft.com/download/dotnet/5.0) (you probably want to use the x64 installer). **Already included if you installed Visual Studio**.
- [Entity Framework .NET CLI](https://docs.microsoft.com/en-us/ef/core/cli/dotnet): install by running this command: `dotnet tool install --global dotnet-ef`.
- To run the web site, you will need to trust the local issued certificate. For that, run this command: `dotnet dev-certs https --trust`. Again, **Visual Studio will deal with that on the first launch of the web site project**.

### On Linux or Mac

I plan to check that but did not have time yet: development on a Mac or Linux machine should work. Feedback welcome.

### Tried and true first run with your local DB when your environment is ready

- Begin with compiling the solution. On a blank machine (I ran some tests on a VM), the first compilation takes several minutes (say, 5) because of the very heavy NuGet package restorations. I have seen it failing, and working on the next attempt.
- You should be able to create a local database by runnning this command line in the folder of the `MemCheck.Database` project: `dotnet ef --startup-project ../MemCheck.WebUI database update`. This creates a database with the connexion string read in the file of the project `MemCheck.WebUI`, which should be ok for development (but has no security).
- You need an account, which you create as usually in the web site (project `MemCheck.WebUI`) running on your debugger (follow the `Register` link on the home page). You won't receive the confirmation email since you don't have any account set for mail sending (see `SendGrid` in [appsettings.json](https://github.com/VoltanFr/memcheck/blob/master/MemCheck.WebUI/appsettings.json)).
- You need two changes about your account: you need it to be admin of your local MemCheck, and email-confirmed. This can be done in various ways. Here is how to do that using the project `MemCheck.CommandLineDbClient`:
  1. Open the file `MemCheck.CommandLineDbClient\Engine.cs`, and check in the method `GetPlugin` that the plugin used is `MakeUserAdmin`.
  2. In the file `MemCheck.CommandLineDbClient\ManageDB\MakeUserAdmin.cs`, modify the field `userName` to set it to the user name you created.
  3. Compile and run the `MemCheck.CommandLineDbClient` project. Note that it ends on an exception, because the current implementation of this project is a bit askew. Its execution is a success if it logs `Test done`.
  4. After that, the MemCheck web site must run correctly from your debugger, and you should be able to log in.
- Now, running the web site project, you should be able to log in. You then need to create a language for cards. Since your account is admin, you have an `Admin` link at the bottom of the screen. This is where you can create a language (eg `Français`).
- You're now all set, with a fully functional local deployment of MemCheck!

### Projects

- [MemCheck.Basics](https://github.com/VoltanFr/memcheck/blob/master/MemCheck.Basics/MemCheck.Basics.csproj) contains very rudimentary services, often in order to save writing boilerplate code.
- [MemCheck.Domain](https://github.com/VoltanFr/memcheck/blob/master/MemCheck.Domain/MemCheck.Domain.csproj) contains the fundamental data structures which depict the register of MemCheck. Only data as persisted in the database, no behavior.
- [MemCheck.Database](https://github.com/VoltanFr/memcheck/blob/master/MemCheck.Database/MemCheck.Database.csproj) contains the [MemCheckDbContext](https://github.com/VoltanFr/memcheck/blob/master/MemCheck.Database/MemCheckDbContext.cs) class, which is the entry point for the database. It also includes the [migrations](https://github.com/VoltanFr/memcheck/tree/master/MemCheck.Database/Migrations).
- [MemCheck.Application](https://github.com/VoltanFr/memcheck/blob/master/MemCheck.Application/MemCheck.Application.csproj) defines the algorithms, the way we interact with the database. It returns raw data, describing what to display, not how to display it.
- [MemCheck.WebUI](https://github.com/VoltanFr/memcheck/blob/master/MemCheck.WebUI/MemCheck.WebUI.csproj) is the ASP.NET Core project which defines the web site. It contains as little algorithm as possible, and focuses on the UI. It is the job of the controllers to adapt for display the data delivered by `MemCheck.Application`.
- [AzFunc.Notifier](https://github.com/VoltanFr/memcheck/blob/master/MemCheck.AzFunc.Notifier/MemCheck.AzFunc.Notifier.csproj) is an Azure function project to run notification scheduled tasks.
- [MemCheck.CommandLineDbClient](https://github.com/VoltanFr/memcheck/blob/master/MemCheck.CommandLineDbClient/MemCheck.CommandLineDbClient.csproj) is a tool to manage the database from the command line.

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
