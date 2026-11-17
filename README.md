# Library Management System

The backend API for a university library system: students register and borrow books, and librarians approve accounts, manage the catalog, track returns and handle late fines.

This repository contains the ASP.NET Core API. The Angular frontend isn't included.

**Stack:** ASP.NET Core 8 Web API · Entity Framework Core · SQL Server · JWT authentication · Swagger

## Features

**For students**
- Register an account, which waits for librarian approval before it can log in
- Get an email when the account is created
- Browse books by category and request to borrow them
- See their own borrowing history

**For librarians**
- Approve new accounts, and block or unblock users
- Add and delete books, and organize them into categories and subcategories
- See all borrowing orders and mark books as returned
- Send reminder emails for overdue books
- Calculate late fines automatically: a book is due 10 days after borrowing, then a daily fine is added
- Block users whose unpaid fines pass the limit

## Project structure

```
Library-API/
  Library-API/
    Controllers/   API endpoints (LibraryController)
    Entity/        Book, BookCategory, Order, User
    Context/       EF Core DbContext
    Migrations/    Database migrations
    Services/      JwtService (tokens), EmailService (SMTP)
```

## Running the API locally

**Requirements:** .NET 8 SDK and SQL Server (Express is enough).

1. Set the connection string in `Library-API/Library-API/appsettings.json` if your SQL Server instance isn't `localhost\SQLEXPRESS`.
2. Add the secrets with .NET user secrets, so they never end up in the repository:
   ```bash
   cd Library-API/Library-API
   dotnet user-secrets init
   dotnet user-secrets set "Jwt:Key" "<a random string of at least 32 characters>"
   dotnet user-secrets set "Constants:FromEmail" "<gmail address that sends the emails>"
   dotnet user-secrets set "Constants:AppPassword" "<Gmail app password>"
   ```
3. Create the database and run the API:
   ```bash
   dotnet ef database update
   dotnet run
   ```
4. Open `/swagger` on the URL the API prints to try the endpoints.

CORS allows a frontend running on `http://localhost:4200`, the default Angular dev server.
