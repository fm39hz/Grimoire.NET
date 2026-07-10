---
tags:
    - grimoire/infrastructure
    - technical-specification
---

# Volume IV: Infrastructure & Render Configuration

This document describes the external services, template engines, and deployment environments required to run Grimoire.NET.

---

## 1. Storage Providers

Grimoire supports two storage strategies for files and assets:
1. **Local File System**: Stores cover images and content files directly on the host drive (e.g., local base directory). This is the default for local development.
2. **S3-Compatible Object Storage**: Connects to S3 providers (such as MinIO or AWS S3) for cloud deployments.

All files are structured inside storage folders as:
`series/{SeriesId}/{FileHash}.{extension}`

---

## 2. Rendering & Templating Engine

Grimoire compiles publication formats using the **Scriban** template engine. 

### 2.1. Render Strategy
* **HTML/XHTML Generation**: Text, image, divider, and footnote segments are processed and compiled into XHTML streams.
* **Layout Isolation**: Embedded Scriban templates (e.g. `epub_chapter.scriban`, `epub_volume.scriban`, `epub_intro.scriban`) receive the compiled segment markup and embed it into standard EPUB 3.3 container frameworks.
* **Metadata Injection**: Templates dynamically parse JSONB metadata to insert cover images, publication dates, and ISBN tags.

---

## 3. Configuration & Deployment

### 3.1. Local Environment Config
For local development, the application is configured to run with `LocalStorage` and a local database, eliminating S3 dependencies.

### 3.2. Production Storage Config
In production, storage is configured to write to S3:
* Requires setting `Storage:Type` to `"S3"`.
* Requires configuring endpoint URL, bucket name, access credentials, and region parameter.

### 3.3. Docker Compose
The system is deployable via standard containers:
* **Database**: Runs Alpine-based PostgreSQL.
* **API & Worker**: Runs the ASP.NET Core API host and background worker process.

---

## 4. Book Navigation & TOC Construction

During compilation, Grimoire walks the ordered hierarchy to build the navigation map:
* **Series**: Resolves to the main cover and title metadata.
* **Volumes**: Generates intermediate separator pages displaying volume covers, summaries, and publication details.
* **Chapters**: Connects directly to chapter text contents and builds nested tables of contents (TOC).
