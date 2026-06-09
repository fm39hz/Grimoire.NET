---
tags:
    - fm39hz/project-grimoire
    - "#project-architecture"
---

# VOLUME I: PROJECT ARCHITECTURE (MVP)

## 1\. Định danh Dự án

- **Tên mã:** **GRIMOIRE (MVP)**
- **Loại hình:** Hệ thống Quản lý và Đóng gói Sách số (Digital Archiving & Publishing System).
- **Mô hình:** Local-First, Manual Curation (Biên tập thủ công).

## 2\. Mục tiêu MVP (Success Criteria)

1. **High-Fidelity Storage:** Lưu trữ cấu trúc truyện (Series/Volume/Chapter) và nội dung (Text/Image) với độ chính xác tuyệt đối.
2. **Professional Metadata:** Quản lý thông tin sách (Tác giả, Họa sĩ, Tags, Ảnh bìa) chi tiết như một thư viện số.
3. **Masterful Binding:** Xuất bản ra file **EPUB 3.3** đạt chuẩn thương mại (Kindle/Apple Books compatible), hỗ trợ chú thích (Popup Footnote), và dàn trang đẹp.
4. **Anthology Support:** Khả năng đóng gói hàng chục tập (Volumes) thành một file "Toàn tập" duy nhất.

## 3\. Kiến trúc Tổng quan

- **Mô hình:** Vertical Slice Architecture.
- **Tech Stack:**
  - **Runtime:** .NET 10.
  - **Database:** PostgreSQL 16 + EF Core 10 (JSONB).
  - **Storage:** MinIO (S3) - Lưu trữ Assets.
  - **Jobs:** Hangfire - Xử lý tác vụ đóng gói (Binding).

## 4\. Unified Book Tree

Từ phase tree refactor, cấu trúc sách có một source of truth chung:

```text
BookShelf (logic root, không lưu DB)
└── Series
    └── Volume
        └── Chapter
```

- `BookShelf` chỉ là root logic trong API/DTO, đại diện cho kệ sách hiện tại.
- `BookNode` là node persistent cho hierarchy thật. `Series`, `Volume`, `Chapter` giữ payload riêng như metadata/content, nhưng quan hệ cha-con, `Title`, và `Order` được điều phối qua tree service.
- API cũ (`/series`, `/volume`, `/chapter`) vẫn là compatibility facade. Khi tạo/sửa/xóa/list volume/chapter, service phải đi qua `IBookTreeService` để tránh mỗi flow tự dựng hierarchy riêng.
- Export/import/sync dùng cây canonical làm đường traversal. Các map/list cũ chỉ nên là adapter tạm để giữ renderer hoặc DTO hiện tại không bị breaking.
