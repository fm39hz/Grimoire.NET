---
tags:
    - grimoire/mcp
    - technical-specification
---

# Volume V: Model Context Protocol (MCP) Specification

This document details the interface definitions for the Grimoire Model Context Protocol (MCP) Server, enabling AI assistants to read, translate, and format book text securely.

---

## 1. Editorial Sandboxing (Concept)

To protect publication layouts from AI formatting errors, Grimoire limits AI write permissions to **text segments only**:

```
Chapter
 ├── [Segment 1] TextSegmentModel      <── AI can edit (Raw Text)
 ├── [Segment 2] ImageSegmentModel     <── Read-Only (Protected Image Link)
 ├── [Segment 3] FootnoteSegmentModel  <── Read-Only (Protected Footnote Ref)
```

By separating formatting structures from raw prose, the LLM cannot corrupt image links, footnote markers, or chapter structures.

---

## 2. MCP Schema

The Grimoire MCP Server exposes the following Resources, Tools, and Prompts to the LLM:

### 2.1. Resources
* **Glossary**: `grimoire://series/{seriesId}/glossary` (Retrieves bilingual terms, definitions, and synonym maps for consistent translations).
* **Layout**: `grimoire://chapters/{chapterId}/layout` (Exposes chapter segment types in order to help the AI map context).

### 2.2. Tools

| Tool Name | Parameters | Description |
| :--- | :--- | :--- |
| `list_text_segments` | `chapterId` | Lists all text segment IDs and order indices in a chapter. |
| `read_text_segment` | `segmentId` | Fetches the raw text of a specific segment. |
| `update_text_segment`| `segmentId`, `content` | Overwrites text in a single text segment. |
| `split_text_segment` | `segmentId`, `characterOffset` | Splits a paragraph segment into two adjacent segments. |
| `merge_text_segments`| `segmentIdA`, `segmentIdB` | Merges two adjacent text segments. |

*Write operations on non-text segments (e.g. `ImageSegmentModel`) are rejected by the server.*

### 2.3. Prompts
* **`translation-workflow`**: Directs the AI to translate a chapter segment-by-segment using `list_text_segments` and `update_text_segment`, while consulting `grimoire://series/{seriesId}/glossary` to maintain proper names and terms.

---

## 3. Server-Side Safety Enforcement

1. **Write Locking**: The API blocks all update requests that target non-text segments.
2. **Glossary Verification**: Updates are scanned against the active glossary to verify that approved terms are translated correctly before committing changes.
3. **Session Audit**: All edits are tracked in the database under `IngestionAuditRecord` with the source type labeled as `MCP-Agent`.
