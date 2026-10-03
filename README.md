# KPGJA ASP.NET Core MVC

Converted from the supplied KPGJA HTML front-end.

## Technology

- ASP.NET Core MVC / .NET 10
- Entity Framework Core 10
- SQL Server
- ASP.NET Core Identity
- Bootstrap 5.3.8
- Bootstrap Icons 1.13.1

## Included

- Public KPGJA website pages
- Member registration with database persistence
- Secure Identity login/logout
- Member dashboard
- Admin dashboard
- Admin membership search/review
- Admin approval/status workflow
- Membership number and payment recording from Admin Panel
- Secure document storage outside `wwwroot`
- CNIC/document validation and file-size/type validation
- Existing website assets and design preserved

## Important workflow

Applicant:
Registration -> Submit application -> Pending Review

Admin:
Review -> Approve for Payment -> Record Payment -> Payment Verified -> Approve/Active Member -> Assign Membership No.

Office-use fields are NOT shown on the public registration form. They are managed from the Admin Panel.

## First run

1. Install the .NET 10 SDK.
2. Open `KPGJA.Web.sln` in Visual Studio 2022/2026 or run:
   `dotnet restore`
   `dotnet run`
3. Update `ConnectionStrings:DefaultConnection` in `appsettings.json` for your SQL Server.
4. The application creates the database automatically on first run using `EnsureCreated()`.
5. Change the seeded admin password before production use.

Default development admin:
- Email: `admin@kpgja.org`
- Password: `Admin@12345!`

## Storage

Uploaded membership documents are stored under:
`Storage/MemberDocuments`

They are deliberately outside `wwwroot` so they are not publicly accessible by direct URL.

## Production note

For production deployment, replace the development seed credentials, use EF Core migrations instead of `EnsureCreated()`, enable HTTPS, configure a production SQL Server connection, and add a proper email/SMS/payment provider if required.
