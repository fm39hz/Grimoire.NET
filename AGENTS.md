# Grimoire.NET Agent Guidelines

## Build & Development Commands

- **Build**: `dotnet build` (from solution root or src/Grimoire.Api/)
- **Run**: `dotnet run --project src/Grimoire.Api/`
- **Docker**: `docker-compose up --build`
- **Clean**: `dotnet clean`

## Linting Commands

- **Dotnet format**: `dotnet format` (formats all C# code according to .editorconfig)
- **Lint C# code**: `dotnet format --verify-no-changes` (verifies code formatting compliance)

## Testing Commands

- **Run all tests**: `bun test` (in test/ directory) - uses Bun test runner
- **Run single test file**: `bun test path/to/your/test-file.test.ts`
- **Run tests with watch mode**: `bun test --watch` (if supported)
- **Test framework**: Mocha with Pactum for API testing, Jest available as alternative
- **Test location**: End-to-end tests in test/ directory using Pactum for API testing

## Code Style Guidelines

### Architecture

- **Vertical Slice Architecture**: Feature-oriented organization
- **Dependency Injection**: Constructor injection preferred
- **Async/Await**: Use async patterns throughout
- **Components**: The Collector, The Librarian, The Editor, The Bindery

### C# Style (.editorconfig enforced)

- **Indentation**: Tabs (4 spaces)
- **Braces**: K&R style (no newlines before braces)
- **Naming**:
  - Classes/Interfaces: PascalCase (interfaces prefixed with 'I')
  - Methods/Properties: PascalCase
  - Parameters/Variables: camelCase
  - Private fields: \_camelCase
  - Constants: UPPER_CASE
- **Records**: Use for immutable entities (models)
- **Required**: Use `required` modifier for mandatory properties
- **Init**: Use `init` for immutable properties
- **Expression-bodied**: Prefer for simple methods/properties

### Imports & Organization

- **Using directives**: Inside namespace
- **System imports**: Sorted first
- **File-scoped namespaces**: Preferred
- **Partial classes**: Avoid unless necessary

### Error Handling

- **Exceptions**: Use specific exception types
- **Null checks**: Use null-conditional operators (`?.`)
- **Validation**: Use DataAnnotations for model validation

### Database & EF Core

- **JSONB**: Use for flexible metadata and content storage
- **Polymorphic segments**: TextRun, ImageSegment, DividerSegment with JsonPolymorphic
- **Navigation properties**: Define relationships explicitly
- **Migrations**: Use EF Core CLI when needed
- **Snake case**: Database naming convention

### API Design

- **RESTful**: Follow REST conventions
- **DTOs**: Separate request/response models
- **Validation**: Use FluentValidation or DataAnnotations
- **Documentation**: XML comments for public APIs
- **Endpoints**: Collector (upload/import), Librarian (metadata), Editor (content), Bindery (export)

### File Organization

- **Models**: Domain/Entity/Book/ (SeriesModel, VolumeModel, ChapterModel, AssetModel)
- **Services**: Application/Service/ (Contract/, Implementation/)
- **Repositories**: Domain/Common/Repository/
- **Controllers**: Api/Controller/
- **DTOs**: Application/Dto/
- **Templates**: RazorLight templates for EPUB rendering

### Comments

- **XML docs**: Required for public APIs
- **BOILERPLATE**: Mark MovieTheater template code with this comment
- **Inline**: Use sparingly, prefer self-documenting code

## Development Best Practices

- **Hot reload**: Use `bun --hot` for development when applicable
- **Type safety**: Leverage TypeScript's strict typing features
- **Consistent formatting**: Always run `dotnet format` before committing C# code
- **Async operations**: Prefer async/await for all I/O operations
- **Configuration**: Use appsettings.json for application configuration
- **Logging**: Use Serilog for structured logging as configured in the project

## Project Structure

- **src/**: Source code organized in vertical slices
- **src/Grimoire.Api/**: Main API project with controllers
- **src/Grimoire.Application/**: Application layer with business logic
- **src/Grimoire.Domain/**: Domain models and entities
- **src/Grimoire.Infrastructure/**: Infrastructure implementations
- **test/**: Integration and end-to-end tests using Bun ecosystem

