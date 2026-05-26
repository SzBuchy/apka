# EventManageApp - AI Coding Agent Instructions

## Project Overview
EventManageApp is an ASP.NET Core 9 MVC web application for managing events and tasks. The project uses a standard MVC architecture with Controllers, Views, and Models.

## Architecture

### Directory Structure
- **Controllers/** - Contains MVC controllers (currently `HomeController`)
- **Models/** - Domain models: `Account` (abstract base), `Admin` (inherits Account), `Task`, `ErrorViewModel`
- **Views/** - Razor templates organized by controller (`Home/`, `Shared/`)
- **wwwroot/** - Static assets (CSS, JavaScript, Bootstrap, jQuery libraries)

### Key Design Patterns
- **Inheritance**: `Admin` extends `Account` base class with role-based model structure
- **MVC Standard Route**: Default routing pattern is `{controller=Home}/{action=Index}/{id?}` (see [Program.cs](../Program.cs#L23))
- **Tag Helpers**: Uses ASP.NET Core tag helpers for view generation (see [Views/_ViewImports.cshtml](../Views/_ViewImports.cshtml))

## Build & Run

### Build
```bash
dotnet build EventManageApp.csproj
```

### Run (Development)
Development profile runs on **http://localhost:5022** and **https://localhost:7033**:
```bash
dotnet run
```
The app auto-launches the browser and sets `ASPNETCORE_ENVIRONMENT=Development`.

### Key Configuration Files
- [Program.cs](../Program.cs) - Service registration and middleware pipeline
- [Properties/launchSettings.json](../Properties/launchSettings.json) - HTTP/HTTPS ports and launch profiles
- [appsettings.json](../appsettings.json) - Base logging configuration
- [appsettings.Development.json](../appsettings.Development.json) - Development overrides

## Development Conventions

### Models
- Located in [Models/](../Models/) namespace `EventManageApp.Models`
- Use file-scoped namespaces (`namespace EventManageApp.Models;`)
- Properties use auto-properties with `{ get; set; }`
- Default values: `Task.Points` defaults to 0m; `Account.Role` defaults to "User"

### Controllers
- Inherit from `Controller` base class
- Dependency injection via constructor (see `ILogger<T>` pattern in [HomeController.cs](../Controllers/HomeController.cs))
- Return `IActionResult` from action methods
- Use `[ResponseCache]` attribute for caching configuration

### Views
- Use Razor syntax (.cshtml files)
- Reference models via `@model` directive
- Access `ViewData` for passing data from controller
- Share common layout via [Shared/_Layout.cshtml](../Views/Shared/_Layout.cshtml)

## Project Details
- **Target Framework**: .NET 9.0
- **Nullable**: Enabled (strict null checking)
- **Implicit Usings**: Enabled (global using statements auto-generated)
- **Dependencies**: Bootstrap, jQuery, jQuery-validation (included in wwwroot/lib)

## Common Tasks

### Adding a New Controller
1. Create `[Feature]Controller.cs` in [Controllers/](../Controllers/) inheriting from `Controller`
2. Create corresponding folder in [Views/](../Views/) for views
3. Add action methods returning `IActionResult` with corresponding `.cshtml` views

### Adding a New Model
1. Create class file in [Models/](../Models/) using file-scoped namespace
2. If role-based, inherit from `Account`; otherwise, use standalone class
3. Use auto-properties for all data members

### Adding Views
1. Create `.cshtml` files in `Views/[ControllerName]/` folder
2. Use tag helpers for HTML generation: `asp-controller`, `asp-action`, `asp-append-version`
3. Reference shared layout layout in [Shared/_Layout.cshtml](../Views/Shared/_Layout.cshtml) for consistency
