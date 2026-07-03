# CliniKa
# Clean Architecture APIs

A backend solution built with **.NET 9**, implementing Clean Architecture for clean separation of concerns, scalability, and maintainability.

## 🏗️ Architecture & Project Structure

The solution consists of the following structured layers:

*   **`CliniKa.Presentation`** (Presentation Layer): ASP.NET Core Web API project exposing endpoints to interact with the system.
*   **`CliniKa.Application`** (Application Layer): Contains application business logic, DTOs, interfaces, and validators.
*   **`CliniKa.Infrastructure`** (Infrastructure Layer): Handles database context, data access (EF Core), external integrations (SendGrid, JWT generation), and infrastructure services.
*   **`CliniKa.Domain`** (Domain Layer): Holds enterprise business rules, repository interfaces, and domain abstractions.

## 🚀 Tech Stack

*   **Framework:** .NET 9
*   **Language:** C#
*   **Architecture:** Clean Architecture

## 🛠️ Getting Started

### Prerequisites

*   [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
*   IDE (Visual Studio 2022, JetBrains Rider, or VS Code)
*   SQL Server

### Installation & Setup

1.  **Restore dependencies:**
    ```bash
    dotnet restore
    ```

2.  **Apply Database Migrations** (ensure your connection string in `appsettings.json` is configured):
    ```bash
    dotnet ef database update --project CliniKa.Infrastructure --startup-project CliniKa.Presentation
    ```

3.  **Run the application:**
    ```bash
    dotnet run --project CliniKa.Presentation
    ```

## 🔌 API Documentation

Once the application is running, navigate to the Swagger UI to view and test the API endpoints:
*   `https://localhost:<port>/swagger`
