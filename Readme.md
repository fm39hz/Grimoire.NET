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
- **Storage**: MinIO (S3-compatible object storage)
- **Jobs**: Hangfire for background processing
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

## API Features

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

### Local Development

- Use Makefile for common tasks:
  - Start/stop database
  - Build and run the application
  - Clean up resources

## Getting Started

1. Clone the repository
2. Run `docker compose up --build` to start all services
3. Access the API at `http://localhost:5062`
4. MinIO console at `http://localhost:9001`
5. PostgreSQL at `localhost:5432` (user: admin, password: admin, database: grimoire)

## Project Structure

- `doc/`: Documentation and specifications
- `src/`: Source code (API, Application, Domain, Infrastructure layers)
- `templates/`: Razor templates for EPUB rendering
