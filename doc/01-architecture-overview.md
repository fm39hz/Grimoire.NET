---
tags:
    - grimoire/architecture
---

# Volume I: System Architecture Overview

This document describes the conceptual architecture and functional workflow of the Grimoire digital book management system.

---

## 1. System Context

Grimoire is designed to ingest raw book manuscripts, organize them into a structured repository, and compile them into reader-ready formats (such as EPUB).

```
   [ Markdown Vault / Scrapers ]
                 │
                 ▼ (Sync/Import)
          [ Grimoire Server ]  <─── (Safe Edit Protocol) ───> [ AI Assistant ]
                 │
                 ▼ (Compile)
         [ EPUB 3.3 Publication ]
```

### Core Value Proposition
* **Safe AI Editing**: Splitting chapters into independent segments ensures that AI assistants can edit prose or perform translations without the ability to corrupt layouts, delete chapters, or break image references.
* **Flexible Sync**: Syncs local folder drafts with the database automatically.

---

## 2. Book Structure

Grimoire organizes content in a logical tree:

```text
Book Collection (Series)
 └── Volumes (Individual Books)
      └── Chapters
           └── Segments (Content Blocks)
```

### Ordering & Restructuring
* **Positioning**: Content nodes maintain their own ordering. When a new chapter or segment is inserted, the system determines its position based on adjacent items, allowing fast insertions and restructurings.

---

## 3. Ingestion & Publishing Pipelines

### 3.1. Ingestion Workflow
When content is imported (e.g. from an Obsidian vault or scraper):
1. **Decomposition**: The raw text (Markdown/HTML) is parsed and separated into structured text segments, image references, and footnotes.
2. **Reconciliation**: Image locations and ownership are calculated to ensure correct folder structure.
3. **Ledger Logging**: Each session outcome is logged to trace import histories.

### 3.2. Publishing Workflow
When compiling a book:
1. **Context Assembly**: Gathers all chapters, volume separators, and cover art within the target structure.
2. **Compilation**: Evaluates templates and packages the contents, stylesheets, and metadata into a standardized EPUB file.
