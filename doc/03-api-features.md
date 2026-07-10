---
tags:
    - grimoire/api
    - technical-specification
---

# Volume III: REST API Features

All endpoints reside under the base routing path `/api/v1/`.

---

## 1. File API
Manages media asset uploads (covers, illustrations) and storage downloads.

### 1.1. Upload File
* **Endpoint**: `POST /api/v1/file/series/{seriesId}`
* **Request Format**: `multipart/form-data`
* **Query Parameters**:
  * `refType` (optional): `"Content"` or `"Cover"` (default: `"Content"`)
* **Response**: Returns asset key and hash details on success.

### 1.2. Get File Stream
* **Endpoint**: `GET /api/v1/file/{assetId}`
* **Response**: Stream of the file content with appropriate MIME headers.

### 1.3. Delete File
* **Endpoint**: `DELETE /api/v1/file/{assetId}`
* **Response**: `204 No Content`

---

## 2. Series API
Manages book collection metadata and structure trees.

### 2.1. List Series
* **Endpoint**: `GET /api/v1/series`
* **Query Parameters**: `pageIndex`, `pageSize`

### 2.2. Get Series Detail
* **Endpoint**: `GET /api/v1/series/{id}`

### 2.3. Get Series Content (Description)
* **Endpoint**: `GET /api/v1/series/{id}/content`
* **Query Parameters**:
  * `format` (optional): `"markdown"` or `"html"` (default: `"markdown"`)

### 2.4. Get Series Book Tree
* **Endpoint**: `GET /api/v1/series/{id}/tree`
* **Description**: Returns the entire book structure (volumes, chapters, and relative orders) in a nested tree JSON.

### 2.5. Create/Sync Series
* **Endpoints**:
  * `POST /api/v1/series` (Create series)
  * `POST /api/v1/series/{id}/sync` (Reorder and sync structural paths)

### 2.6. Update / Delete Series
* **Endpoints**:
  * `PATCH /api/v1/series/{id}`
  * `DELETE /api/v1/series/{id}`

---

## 3. Volume API
Manages individual book volumes.

### 3.1. Create / Update / Delete Volume
* **Endpoints**:
  * `POST /api/v1/volume`
  * `PATCH /api/v1/volume/{id}`
  * `DELETE /api/v1/volume/{id}`

### 3.2. Get Volume Chapters
* **Endpoint**: `GET /api/v1/volume/{id}/chapters`

---

## 4. Chapter API
Manages chapter metadata and structural edits.

### 4.1. Get Chapter Detail & Content
* **Endpoints**:
  * `GET /api/v1/chapter/{id}`
  * `GET /api/v1/chapter/{id}/content` (Returns rendered Markdown or HTML)

### 4.2. Create / Update Chapter
* **Endpoints**:
  * `POST /api/v1/chapter` (Creates chapter and parses content into segments)
  * `PATCH /api/v1/chapter/{id}` (Updates metadata or replaces segments)

### 4.3. Split / Merge Chapters
* **Endpoints**:
  * `POST /api/v1/chapter/{id}/split` (Splits chapter at designated segment positions)
  * `POST /api/v1/chapter/merge` (Combines multiple chapters into one)

---

## 5. Segment API (AI Sandboxed Operations)
Provides fine-grained access to individual content segments within chapters.

### 5.1. List Chapter Segments
* **Endpoint**: `GET /api/v1/chapters/{chapterId}/segments`
* **Response**: Ordered list of segments, showing types and content previews.

### 5.2. Get / Update Segment
* **Endpoints**:
  * `GET /api/v1/segments/{id}` (Read segment text or asset metadata)
  * `PUT /api/v1/segments/{id}` (Updates text. Allowed on text segments only)

### 5.3. Split / Merge Segments
* **Endpoints**:
  * `POST /api/v1/segments/{id}/split` (Splits a text paragraph segment at a character offset)
  * `POST /api/v1/segments/merge` (Combines adjacent segments)

---

## 6. Ingestion Audit API
Retrieves history logs of sync and ingestion sessions.

### 6.1. Get Ingestion History
* **Endpoint**: `GET /api/v1/ingestion-audits/series/{seriesId}`
* **Query Parameters**: `limit` (default: 20)

---

## 7. Publish API
Handles background publication compiles.

### 7.1. Export Series Anthology
* **Endpoint**: `POST /api/v1/publish/export?seriesId={seriesId}`
* **Response**: `202 Accepted` with Background Job tracking ID.

### 7.2. Import EPUB
* **Endpoint**: `POST /api/v1/publish/import`
* **Response**: `202 Accepted` with Background Job tracking ID.

### 7.3. Job Progress Stream
* **Endpoint**: `GET /api/v1/publish/jobs/{jobId}/progress` (Server-Sent Events stream)

### 7.4. Download Export Result
* **Endpoint**: `GET /api/v1/publish/jobs/{jobId}/download`
