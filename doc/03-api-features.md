---
tags:
    - fm39hz/project-grimoire
    - api
---

# VOLUME III: FEATURE SPECS & API (MVP v1.1)

## Feature 1: The Collector (Nhập liệu)

### 1.1. Upload Asset (Smart Upload)

API nhận file ảnh từ Client. Có cơ chế check trùng để tiết kiệm dung lượng và băng thông.

- **Endpoint:** `POST /api/assets/upload`
- **Query Params:** `seriesId={uuid}&fileHash={md5}`
- **Body:** `multipart/form-data` (File Content)
- **Logic:**
    1. **Check Exist:** Query bảng `Assets` xem có `SeriesId` và `FileHash` này chưa.
        - Nếu có: Trả về `200 OK` + `AssetKey` cũ ngay lập tức (Không upload lại).
    2. **Upload:** Nếu chưa, stream file lên MinIO.
    3. **Record:** Lưu thông tin vào bảng `Assets`.
    4. **Return:** Trả về `AssetKey` mới.

### 1.2. Import Raw Chapter

Nhận dữ liệu JSON từ Tool Scraper bên ngoài.

- **Endpoint:** `POST /api/v1/import/chapter`
- **Body:** JSON cấu trúc `ChapterRequest`.
- **Logic:**
  - Validate cấu trúc.
  - Lưu `TextRun` vào DB.

## Feature 2: The Librarian (Quản lý)

### 2.1. Update Metadata

- **Endpoint:** `PUT /api/v1/volumes/{id}/metadata`
- **Chức năng:** Cập nhật ảnh bìa, tóm tắt, tác giả cho từng tập.

## Feature 3: The Editor (Biên tập Thủ công)

### 3.1. Get Content

- **Endpoint:** `GET /api/v1/chapters/{id}/content`

### 3.2. Update Segment

- **Endpoint:** `PUT /api/v1/chapters/{id}/segments/{segmentId}`

## Feature 4: The Grand Bindery (Xuất bản)

### 4.1. Bind Series (Job)

- **Endpoint:** `POST /api/v1/bindery/series/{id}/bind`
- **Payload:**

    ```json
    {
     "mode": "Anthology",
     "targetVolumeIds": ["uuid-1"],
     "injectCss": true
    }
    ```

- **Logic (Hangfire Worker):**
    1. Fetch Data.
    2. Render HTML (Sử dụng Template Engine).
    3. Package EPUB.
    4. Upload to MinIO.

