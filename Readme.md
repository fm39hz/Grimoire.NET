# Grimoire.NET

## Overview

**GRIMOIRE** is a Digital Archiving & Publishing System designed for managing and publishing digital books. It focuses on high-fidelity storage of book structures (Series/Volume/Chapter) and content (Text/Image), professional metadata management, and masterful binding into EPUB 3.3 format compatible with commercial readers like Kindle and Apple Books.

### Key Features

- **High-Fidelity Storage**: Precise storage of book structures and content with absolute accuracy.
- **Professional Metadata**: Detailed management of book information including authors, artists, tags, and cover images.
- **Masterful Binding**: Export to EPUB 3.3 standard with support for footnotes, beautiful layout, and anthology packaging.
- **Anthology Support**: Ability to package multiple volumes into a single "Complete Collection" file.
- **Local-First, Manual Curation**: Designed for local operation with manual editing capabilities.

## Tech Stack

- **Runtime**: .NET 9
- **Database**: PostgreSQL 16 + EF Core 9 (JSONB for flexible metadata)
- **Storage**: S3-compatible object storage
- **Jobs**: Background processing service
- **Rendering**: RazorLight for HTML template rendering

## Architecture

The system follows a Vertical Slice Architecture with the following components:

- **The Collector**: Handles data import (asset uploads, chapter imports)
- **The Librarian**: Manages metadata updates for series and volumes
- **The Bindery**: Processes EPUB export jobs

## Database Schema

### Core Entities

- **Series**: Represents a book series with metadata (title, authors, tags, description, cover)
- **Volume**: Individual volumes within a series (order, title, metadata)
- **Chapter**: Content chapters with structured segments
- **Assets**: File management for images and covers

### Content Structure

Content is stored using JSONB for flexibility:

- **Metadata**: Flexible book information (authors, artists, tags, etc.)
- **Segments**: Polymorphic content units (Text, Image, Divider)
- **Text Segments**: Rich text with formatting (bold, italic, footnotes)

## Getting Started

### Deployment with Docker

1. Clone the repository
2. Run `docker compose up --build` to start all services
3. Access the API at `http://localhost:5062`
4. PostgreSQL at `localhost:5432` (user: admin, password: admin, database: grimoire)

### Local Development

- Use Makefile for common tasks:
  - Start/stop database
  - Build and run the application
  - Clean up resources

## Project Technical Highlights

- **Core Architecture**: Clean architecture with Domain, Application, Infrastructure, and API layers
- **Database**: PostgreSQL with EF Core, JSONB support for flexible metadata
- **Storage**: S3-compatible integration for asset management
- **CRUD Operations**: Full CRUD for Series, Volumes, and Chapters
- **File Management**: Upload, retrieve, and delete assets with deduplication
- **Data Import**: Chapter import with preprocessed and raw markdown ingestion strategies
- **Export System**: EPUB 3.3 export with full metadata, footnotes, and anthology support
- **HTML Export**: Generate HTML files from book content
- **Validation**: FluentValidation for all DTOs
- **Testing**: E2E test suite for full workflow

## Milestones

- [x] Series/Volume/Chapter CRUD operations
- [x] Asset management and storage
- [x] Basic metadata handling
- [x] EPUB 3.3 export
- [x] Import strategies for chapter content
- [x] HTML export
- [ ] PDF export implementation
- [ ] Background job processing
- [ ] Comprehensive error handling
- [ ] API documentation
- [ ] Performance optimization
- [ ] Production deployment guide
- [ ] Search and filtering
- [ ] Bulk operations
- [ ] Content preview
- [ ] Asset optimization
- [ ] Enhanced validation and business rules

## Known Issues

1. **RawMarkdownIngestionStrategy** - SeriesId is hardcoded to Guid.Empty (src/Grimoire.Application/Service/Strategy/RawMarkdownIngestionStrategy.cs:54)
2. **PdfExportStrategy** - Not implemented yet (src/Grimoire.Infrastructure/Export/PdfExportStrategy.cs:18)

## Contributing

Contributions are welcome! Please ensure:

- All tests pass before submitting PR
- Follow existing code style and architecture patterns
- Update documentation for new features
- Add unit tests for new functionality

## Project Structure

- `doc/`: Documentation and specifications
- `src/`: Source code (API, Application, Domain, Infrastructure layers)
- `test/`: E2E test suite with sample assets
- `templates/`: Razor templates for EPUB rendering (if exists)
