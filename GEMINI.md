# Project: Grimoire.NET

## Project Overview

**Grimoire.NET** is a Digital Archiving & Publishing System designed for managing and publishing digital books. It focuses on high-fidelity storage of book structures (Series/Volume/Chapter) and content (Text/Image), professional metadata management, and masterful binding into EPUB 3.3 format compatible with commercial readers like Kindle and Apple Books.

The project is built with .NET 9 and follows a Vertical Slice Architecture, which is similar to Clean Architecture. It uses PostgreSQL for the database and MinIO for object storage.

The solution is divided into four projects:

- `Grimoire.Api`: The presentation layer, containing the API controllers.
- `Grimoire.Application`: The application layer, containing services and DTOs.
- `Grimoire.Domain`: The domain layer, containing entities and repository interfaces.
- `Grimoire.Infrastructure`: The infrastructure layer, containing repository implementations, database context, and other infrastructure-related code.

## Building and Running

The project is containerized using Docker. To build and run the application, use the following command:

```bash
docker-compose up --build
```

This will start the following services:

- **PostgreSQL database:** Accessible at `localhost:5432`
- **MinIO storage:** Accessible at `http://localhost:9000` (API) and `http://localhost:9001` (console)
- **Grimoire.NET API:** Accessible at `http://localhost:5000`

## Development Conventions

### Coding Style

The project uses the `.editorconfig` file to enforce coding style. Some key conventions are:

- **Indentation:** Tabs with a size of 4.
- **Braces:** C# braces are preferred to be on the same line.
- **`var` keyword:** `var` is preferred for built-in types, when the type is apparent, and elsewhere.
- **Naming:**
  - Interfaces are prefixed with `I` (e.g., `IMovieService`).
  - Async methods should have an "Async" suffix.
  - Constants are in `UPPER_CASE`.
  - Non-public fields are `_camelCase`.
  - Public fields are `PascalCase`.

### Architecture

The project follows a Vertical Slice Architecture. This means that the code is organized by feature (or "slice") rather than by layer. Each slice contains all the code necessary to implement that feature, from the API controller to the database access.

### Testing

There are no tests in the project at the moment. When adding new features, it is recommended to also add unit and integration tests.
