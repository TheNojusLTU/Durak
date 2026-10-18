# Job Application Tracker

A web application for tracking job applications throughout the hiring process.

This project is being developed for a Software Engineering course using **C#** and **ASP.NET Core**.

## Features

Planned features include:

- User registration and login
- Create, view, edit, and delete job applications
- Application status tracking
- Application status history
- Interview management
- Search and filtering
- Dashboard with application overview
- Job search statistics
- CV and cover letter uploads
- Favorite and priority applications

## Application Statuses

Job applications can move through several stages:

- Saved
- Applied
- Interview
- Offer
- Rejected
- Withdrawn

## Local Database Setup

The server uses PostgreSQL. Install and start PostgreSQL locally, then create a database named `jobtracker` (for example, with `createdb jobtracker`). Set the connection string with .NET user secrets from the repository root:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=jobtracker;Username=postgres;Password=YOUR_LOCAL_PASSWORD" --project JobTracker/JobTracker.csproj
```

The project already has a `UserSecretsId`, so `dotnet user-secrets init` is not needed. User secrets are stored outside the repository; do not commit database passwords. For a hosted PostgreSQL database, use the provider's host, port, database, and username, and add `SSL Mode=Require` when required by the provider.

Apply the schema and run the app:

```bash
dotnet ef database update --project JobTracker/JobTracker.csproj --startup-project JobTracker/JobTracker.csproj
dotnet run --project JobTracker/JobTracker.csproj
```

The initial migration creates the Identity tables and `JobApplications`, with each job linked to its owning user.

## Account Registration

Registration uses a unique username (3–20 characters) and password; display name is required (3–20 characters), while email is optional. Email confirmation is disabled for now. Passwords must be at least 8 characters and contain an uppercase letter, a lowercase letter, and a number. ASP.NET Core Identity stores password hashes, and a successful registration signs the user in and redirects to their profile.

## React Frontend
Prerequisite: Node.js 22.x (tested with v22.23.1) and npm 10 or newer.

The separate React + TypeScript frontend is in `JobTracker.Web` and uses Vite. It is currently a static UI scaffold; API integration and account/application behavior are not included yet.

Install dependencies and start the development server from the repository root:

```bash
npm --prefix JobTracker.Web install
npm --prefix JobTracker.Web run dev
```

Open the local URL printed by Vite. Check the frontend with ESLint and a production build:

```bash
npm --prefix JobTracker.Web run lint
npm --prefix JobTracker.Web run build
```
