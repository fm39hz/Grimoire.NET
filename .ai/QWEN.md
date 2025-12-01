# Grimoire.NET - Digital Archiving & Publishing System

## Project Overview

**GRIMOIRE** is a Digital Archiving & Publishing System designed for managing and publishing digital books. It focuses on high-fidelity storage of book structures (Series/Volume/Chapter) and content (Text/Image), professional metadata management, and masterful binding into EPUB 3.3 format compatible with commercial readers like Kindle and Apple Books.

### Key Features

- **High-Fidelity Storage**: Precise storage of book structures and content with absolute accuracy
- **Professional Metadata**: Detailed management of book information including authors, artists, tags, and cover images
- **Masterful Binding**: Export to EPUB 3.3 standard with support for footnotes, beautiful layout, and anthology packaging
- **Anthology Support**: Ability to package multiple volumes into a single "Complete Collection" file
- **Local-First, Manual Curation**: Designed for local operation with manual editing capabilities

### Tech Stack

- **Runtime**: .NET 9
- **Database**: PostgreSQL 16 + EF Core 9 (JSONB for flexible metadata)
- **Storage**: MinIO (S3-compatible object storage)
- **Jobs**: Hangfire for background processing
- **Rendering**: RazorLight for HTML template rendering
- **Architecture**: Vertical Slice Architecture

## Project Structure

- `src/`: Contains the main source code divided into layered architecture
  - `Grimoire.Api`: API layer and web application entry point
  - `Grimoire.Application`: Application services and business logic
  - `Grimoire.Domain`: Core domain models and entities
  - `Grimoire.Infrastructure`: Infrastructure implementations (database, storage, etc.)
- `doc/`: Documentation and specifications
- `templates/`: Razor templates for EPUB rendering
- Root directory: Configuration files (Docker, Makefile, solution file)

## Building and Running

### Prerequisites
- .NET 9 SDK
- Docker and Docker Compose
- Make (optional, but recommended)

### Development Commands

**Using Makefile:**
```bash
# Start all services (PostgreSQL, MinIO)
make db-up

# Build the application
make build

# Run the application
make run

# Run tests
make test

# Clean build artifacts
make clean

# Stop database
make db-down
```

**Using Docker Compose:**
```bash
# Build and start all services (PostgreSQL, MinIO, API)
docker compose up --build

# Start services in detached mode
docker compose up -d
```

**Direct .NET Commands:**
```bash
# Build
dotnet build

# Run the API
dotnet run --project src/Grimoire.Api

# Run tests
dotnet test
```

### Environment Configuration

The application expects the following services:
- PostgreSQL at `localhost:5432` (user: admin, password: admin, database: grimoire)
- MinIO at `localhost:9000` (console at `localhost:9001`, credentials: admin/password)

## Architecture Components

### Core Services (The Three Pillars)
1. **The Collector**: Handles data import (asset uploads, chapter imports)
2. **The Librarian**: Manages metadata updates for series and volumes
3. **The Bindery**: Processes EPUB export jobs

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

## API Endpoints

### The Collector (Data Import)
- `POST /api/assets/upload`: Smart upload with deduplication
- `POST /api/v1/import/chapter`: Import raw chapter data

### The Librarian (Metadata Management)
- `PUT /api/v1/volumes/{id}/metadata`: Update volume metadata

### The Editor (Manual Editing)
- `GET /api/v1/chapters/{id}/content`: Retrieve chapter content
- `PUT /api/v1/chapters/{id}/segments/{segmentId}`: Update content segments

### The Bindery (Publishing)
- `POST /api/v1/bindery/series/{id}/bind`: Export series to EPUB

## Infrastructure

### Storage (MinIO)
- `grimoire-assets`: Source assets (covers, content images)
- `grimoire-exports`: Generated EPUB files

### Rendering Engine
Uses RazorLight with CSHTML templates for consistent HTML generation:
- `Chapter.cshtml`: Chapter content rendering
- `Intro.cshtml`: Book introduction and metadata
- `toc.cshtml`: Table of contents

## Development Conventions

- The project follows Vertical Slice Architecture
- Uses domain-driven design principles
- Implements JSONB for flexible metadata storage
- Employs async/await patterns for I/O operations
- Uses structured logging for better observability
- Follows C# coding conventions with nullable reference types enabled

## Local Development

1. Clone the repository
2. Start dependencies: `make db-up` or `docker compose up -d`
3. Build: `make build` or `dotnet build`
4. Run: `make run` or `dotnet run --project src/Grimoire.Api`
5. Access the API at `http://localhost:5062`
6. MinIO console at `http://localhost:9001`
7. PostgreSQL at `localhost:5432`