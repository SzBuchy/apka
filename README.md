# FieldGameApp

FieldGameApp is an ASP.NET Core MVC application for running event-based field games. It supports participant tasks, answer and media submissions, admin review, point-based leaderboards, user coupons, and coupon validation by a scanner account.

## Features

- Cookie-based login with three roles: `Admin`, `User`, and `Scaner`
- Admin panel for creating, editing, and deleting tasks
- User task list with answer submission and optional photo/video upload
- Cloudinary storage for uploaded submission files
- Admin submission review with approve, reject, and decision reset flows
- Automatic point assignment after approved submissions
- Public-style leaderboard views for users and admins
- Coupon generation for one user or all users
- Coupon validation workflow for scanner accounts
- PostgreSQL database access through Entity Framework Core and Npgsql
- Automatic database migration on application startup

## Tech Stack

- ASP.NET Core MVC
- .NET 9
- Entity Framework Core
- PostgreSQL, commonly Supabase-hosted
- Cloudinary
- Bootstrap, jQuery, and Razor views
- Docker

## Project Structure

```text
.
├── Controllers/          # MVC controllers for account, admin, user, scanner, and home flows
├── Data/                 # EF Core DbContext and migrations
├── Models/               # Account, user, task, submission, and coupon models
├── Views/                # Razor views grouped by controller
├── wwwroot/              # Static CSS, JavaScript, and frontend libraries
├── Program.cs            # Application startup, services, auth, migrations, and seed data
├── EventManageApp.csproj # Project dependencies and target framework
└── Dockerfile            # Container build for deployment
```

## Prerequisites

- .NET 9 SDK
- PostgreSQL database
- Cloudinary account
- Optional: Docker

## Configuration

The application expects configuration values for the database, Cloudinary, and Supabase client.

For local development, create an `appsettings.Development.json` file in the project root:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=fieldgameapp;Username=postgres;Password=your_password"
  },
  "Cloudinary": {
    "CloudName": "your_cloud_name",
    "ApiKey": "your_api_key",
    "ApiSecret": "your_api_secret"
  },
  "Supabase": {
    "Url": "https://your-project.supabase.co",
    "Key": "your_supabase_anon_or_service_key"
  }
}
```

You can also provide the same values through environment variables, for example:

```bash
ConnectionStrings__DefaultConnection="Host=...;Port=5432;Database=...;Username=...;Password=..."
Cloudinary__CloudName="..."
Cloudinary__ApiKey="..."
Cloudinary__ApiSecret="..."
Supabase__Url="..."
Supabase__Key="..."
```

## Running Locally

Restore dependencies:

```bash
dotnet restore
```

Run the app:

```bash
dotnet run
```

By default, the development profile uses:

- `http://localhost:5022`
- `https://localhost:7033`

The default route opens the login page at `/Account/Login`.

## Database Migrations and Seed Data

The app applies EF Core migrations automatically when it starts:

```csharp
db.Database.Migrate();
```

If the accounts table is empty, the app seeds:

- Admin account: `admin` / `123`
- Scanner account: `scaner` / `123`
- 200 generated user accounts

Generated user credentials are written to:

```text
generated_users.csv
```

This file is created at runtime in the current working directory.

To add a new migration manually:

```bash
dotnet ef migrations add MigrationName
```

To update the database manually:

```bash
dotnet ef database update
```

## User Roles

### Admin

Admins can:

- Manage tasks
- Review submitted answers and files
- Approve submissions and award points
- Reject submissions with a reason
- Reset previous submission decisions
- Edit leaderboard points and nicknames
- Create and delete coupons

### User

Users can:

- View available tasks
- Submit text answers
- Upload required photos or videos for selected tasks
- View their coupons
- View the leaderboard

### Scaner

Scanner accounts can:

- Validate coupon serial numbers
- Mark valid coupons as used
- View coupon usage statistics grouped by coupon title

## File Uploads

Task submissions can include files uploaded to Cloudinary.

Current upload behavior:

- Files are uploaded as raw Cloudinary assets
- Maximum file size is 50 MB
- Uploaded files are stored in the `event_manage_app_submissions` Cloudinary folder
- The secure Cloudinary URL is stored on the task submission

## Docker

Build the image:

```bash
docker build -t fieldgameapp .
```

Run the container:

```bash
docker run -p 8080:8080 \
  -e ConnectionStrings__DefaultConnection="Host=...;Port=5432;Database=...;Username=...;Password=..." \
  -e Cloudinary__CloudName="..." \
  -e Cloudinary__ApiKey="..." \
  -e Cloudinary__ApiSecret="..." \
  -e Supabase__Url="..." \
  -e Supabase__Key="..." \
  fieldgameapp
```

Then open:

```text
http://localhost:8080
```

The application also reads the `PORT` environment variable when available, which is useful for platforms such as Render.

## Common Commands

```bash
dotnet restore
dotnet build
dotnet run
dotnet ef migrations add MigrationName
dotnet ef database update
docker build -t fieldgameapp .
```

## Notes

- Passwords are currently stored as plain text in the database. Use password hashing before production use.
- The default seeded credentials are intended for development and should be changed before deployment.
- The app depends on a valid PostgreSQL connection string at startup.
- Cloudinary configuration is required for file-based task submissions.
