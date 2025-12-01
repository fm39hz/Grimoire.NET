# Grimoire.NET Agent Guidelines

## Build & Development Commands

- **Build**: `dotnet build` (from solution root or src/Grimoire.Api/)
- **Run**: `dotnet run --project src/Grimoire.Api/`
- **Docker**: `docker-compose up --build` (includes PostgreSQL + MinIO)
- **Clean**: `dotnet clean`

## Testing

No test framework currently configured. Use `dotnet test` when tests are added.

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

### Security

- **Input validation**: Always validate user input
- **Authentication**: JWT Bearer tokens
- **Authorization**: Role-based access control
- **Secrets**: Never commit sensitive data

### Performance

- **Async**: Use async for I/O operations
- **EF Core**: Use AsNoTracking for read-only queries
- **Pagination**: Implement for large datasets
- **Caching**: Consider for frequently accessed data

### EPUB Generation

- **RazorLight**: Use for HTML template rendering
- **EPUB 3.3**: Target format with footnotes and popup support
- **Templates**: Chapter.cshtml, Intro.cshtml, toc.cshtml
- **CSS**: Inject styles.css with epub namespace
- **Hangfire**: Background job processing for binding

