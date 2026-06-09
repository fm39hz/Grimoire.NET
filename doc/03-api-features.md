---
tags:
    - fm39hz/project-grimoire
    - api
---

# VOLUME III: FEATURE SPECS & API (MVP v1.1)

Tất cả các API được thiết kế theo chuẩn RESTful và phân nhóm theo 5 Controller tương ứng với tính năng của hệ thống. Prefix đường dẫn mặc định là: `api/v1/[controller]`

---

## 1. File API (Quản lý File & Media)
**Route Base:** `api/v1/file`

Quản lý luồng tải lên và tải xuống các asset (Cover, Content Images) lưu tại Storage.

### 1.1. Upload File (Smart Upload)
Tải ảnh/asset lên hệ thống. Tự động kiểm tra trùng lặp thông qua Hash (MD5/SHA256) của file để tối ưu bộ nhớ.
- **Endpoint:** `POST /api/v1/file/upload/{seriesId}`
- **Request Type:** `multipart/form-data`
- **Path Parameter:**
  - `seriesId` (string, required): Mã định danh series (dạng `ser_...`).
- **Query Parameter:**
  - `refType` (string, optional): Loại tham chiếu asset (`"Content"` hoặc `"Cover"`). Mặc định là `"Content"`.
- **Response (200 OK):** Trả về DTO thông tin asset chứa `AssetKey` (Path trong S3).

### 1.2. Get File Stream
Tải/Lấy nội dung file ảnh để hiển thị.
- **Endpoint:** `GET /api/v1/file/{assetId}`
- **Response (200 OK):** Stream nhị phân của file cùng với `ContentType` tương ứng.

### 1.3. Delete File
Xóa asset ra khỏi hệ thống.
- **Endpoint:** `DELETE /api/v1/file/{assetId}`
- **Response (204 No Content):** Xóa thành công.

---

## 2. Series API (Quản lý Bộ truyện)
**Route Base:** `api/v1/series`

### 2.1. List Series (Phân trang)
- **Endpoint:** `GET /api/v1/series`
- **Query Parameters:** `pageIndex`, `pageSize`.
- **Response (200 OK):** Danh sách Series kèm thông tin phân trang.

### 2.2. Get Series Detail
- **Endpoint:** `GET /api/v1/series/{id}`
- **Query Parameter:** `timestamp` (bool, optional): Hiển thị ngày tạo/sửa. Mặc định `false`.

### 2.3. Get Series Content (Description)
Lấy tóm tắt bộ truyện đã qua render định dạng.
- **Endpoint:** `GET /api/v1/series/{id}/content`
- **Query Parameter:** `format` (string, optional): `"markdown"` hoặc `"html"`. Mặc định `"markdown"`.

### 2.4. Get Series Book Tree
Lấy toàn bộ cấu trúc cây thư mục (Canonical Book Tree) của Series.
- **Endpoint:** `GET /api/v1/series/{id}/tree`
- **Response (200 OK):** Trả về cấu trúc cây `BookTreeDto` gồm danh sách các volume và chapter con được sắp xếp theo thứ tự chuẩn.

### 2.5. Create/Get Series
Tạo mới series (hoặc trả về series hiện có nếu trùng tiêu đề).
- **Endpoint:** `POST /api/v1/series`
- **Body:** `CreateSeriesRequestDto`
- **Response (201 Created / 200 OK):** Series detail DTO.

### 2.6. Update Series
- **Endpoint:** `PATCH /api/v1/series/{id}`
- **Body:** `UpdateSeriesRequestDto`

### 2.7. Delete Series
- **Endpoint:** `DELETE /api/v1/series/{id}`

### 2.8. Get Series Volumes
- **Endpoint:** `GET /api/v1/series/{id}/volumes`

### 2.9. Sync Series Tree
Đồng bộ hóa thứ tự và liên kết cấu trúc cây từ phía Client.
- **Endpoint:** `POST /api/v1/series/{id}/sync`
- **Body:** `SyncSeriesRequestDto`

---

## 3. Volume API (Quản lý Tập)
**Route Base:** `api/v1/volume`

### 3.1. Create Volume
- **Endpoint:** `POST /api/v1/volume`
- **Body:** `CreateVolumeRequestDto`

### 3.2. Update Volume
- **Endpoint:** `PATCH /api/v1/volume/{id}`
- **Body:** `UpdateVolumeRequestDto`

### 3.3. Delete Volume
- **Endpoint:** `DELETE /api/v1/volume/{id}`

### 3.4. Get Chapters in Volume
- **Endpoint:** `GET /api/v1/volume/{id}/chapters`

---

## 4. Chapter API (Quản lý Chương)
**Route Base:** `api/v1/chapter`

### 4.1. Get Chapter Detail
- **Endpoint:** `GET /api/v1/chapter/{id}`

### 4.2. Get Chapter Content (Rendered)
Lấy nội dung chi tiết của chương đã render sang HTML hoặc Markdown.
- **Endpoint:** `GET /api/v1/chapter/{id}/content`
- **Query Parameter:** `format` (string): `"html"` hoặc `"markdown"`. Mặc định là `"markdown"`.
- **Response (200 OK):**
  ```json
  {
    "data": "nội dung đã render...",
    "type": "text/markdown",
    "assets": []
  }
  ```

### 4.3. Create/Import Chapter
Nhận dữ liệu từ Tool Scraper để tạo mới chương kèm nội dung segments.
- **Endpoint:** `POST /api/v1/chapter`
- **Body:** `CreateChapterRequestDto`

### 4.4. Update Chapter
Cập nhật thông tin tiêu đề, thứ tự hoặc cập nhật mảng segments mới.
- **Endpoint:** `PATCH /api/v1/chapter/{id}`
- **Body:** `UpdateChapterRequestDto`

### 4.5. Split Chapter
Chia một chương thành nhiều chương con dựa vào vị trí segment chỉ định.
- **Endpoint:** `POST /api/v1/chapter/{id}/split`
- **Body:**
  ```json
  {
    "splitPoints": [
      { "segmentIndex": 25, "newChapterTitle": "Phần II" }
    ]
  }
  ```

### 4.6. Merge Chapters
Gộp nhiều chương lại thành một chương duy nhất.
- **Endpoint:** `POST /api/v1/chapter/merge`
- **Body:** `MergeChaptersRequestDto`

---

## 5. Publish API (Đóng gói & Xuất bản)
**Route Base:** `api/v1/publish`

Hệ thống xử lý xuất/nhập sách dưới nền (Background Job) thông qua Hangfire Worker.

### 5.1. Export Series Anthology (Publish Job)
Tạo task đóng gói bộ truyện hoặc tập truyện thành EPUB hoặc các định dạng khác.
- **Endpoint:** `POST /api/v1/publish/export?seriesId={seriesId}`
- **Body:**
  ```json
  {
    "format": "Epub",
    "mode": "Anthology",
    "targetVolumeIds": [],
    "structure": null
  }
  ```
- **Response (202 Accepted):** Trả về ID của Background Job và URL theo dõi trạng thái.

### 5.2. Import EPUB Book
Nhập và phân tách tự động file EPUB tải lên thành Series/Volume/Chapter tương ứng trong DB.
- **Endpoint:** `POST /api/v1/publish/import`
- **Request Type:** `multipart/form-data`
- **Body (Form):**
  - `series`: Metadata JSON string của Series.
  - `volumes`: (Optional) Metadata JSON string ghi đè cho các Volume.
  - `file`: File `.epub` cần import.
- **Response (202 Accepted):** Trả về ID của Import Background Job.

### 5.3. Get Job Status
- **Endpoint:** `GET /api/v1/publish/jobs/{jobId}`

### 5.4. Download Export Result
Tải xuống thành phẩm (EPUB/ZIP) sau khi Job đóng gói hoàn thành.
- **Endpoint:** `GET /api/v1/publish/jobs/{jobId}/download`
- **Response (200 OK):** File Stream kết quả tải xuống.
