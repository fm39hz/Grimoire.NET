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

---

## 5. Algorithmic Tree Foundation & Solution Structure

Kiến trúc cây dữ liệu trong Grimoire.NET không chỉ để tổ chức dữ liệu mà còn đóng vai trò là nền tảng thuật toán cốt lõi cho mọi thao tác cấu trúc:

### 5.1. Phân Tách Cấu Trúc (Structure) & Dữ Liệu (Payload)
- **Độc lập lưu trữ:** Bảng `book_nodes` chỉ lưu cấu trúc hình học của cây (như `ParentId`, `Order`, `Title`, `Type`). Toàn bộ dữ liệu nặng/payload (như metadata JSONB, segments văn bản, hình ảnh, footnotes) được lưu ở các bảng chuyên biệt (`Series`, `Volume`, `Chapter`, `ChapterContent`).
- **Lợi ích:** Các thao tác duyệt cây, sắp xếp, di chuyển node diễn ra vô cùng nhanh chóng trên bảng `book_nodes` có kích thước nhỏ mà không cần tải hay ghi nhận lại hàng Megabyte nội dung văn bản.

### 5.2. Thuật Toán Fractional Ordering (Sắp xếp Số thực)
- **Vấn đề:** Trong sắp xếp truyền thống bằng số nguyên (`int`), khi chèn một chương vào giữa chương 1 và chương 2, hệ thống phải cập nhật lại chỉ mục (`Order = Order + 1`) cho hàng trăm chương phía sau, dẫn tới thao tác ghi cơ sở dữ liệu rất tốn kém.
- **Giải pháp:** Hệ thống sử dụng số thực (`float`) cho trường `Order`. Khi di chuyển hoặc chèn node vào giữa hai node có vị trí `A` và `B`, thuật toán chỉ cần nội suy vị trí mới bằng trung bình cộng `(A + B) / 2` (ví dụ: `1.5` nằm giữa `1.0` và `2.0`). Thao tác chèn chỉ tốn 1 câu lệnh cập nhật duy nhất cho chính node đó.

### 5.3. Quy Tắc Ràng Buộc & Duyệt Cây (Tree Invariants & Traversal)
- **Enforced Invariants (Ràng buộc cứng):** Khi thao tác trên cây, `IBookTreeService` thực hiện validate nghiêm ngặt:
  - Series luôn là Root-level (không có parent).
  - Volume chỉ được có parent là Series.
  - Chapter chỉ được có parent là Volume (Chapter là lá, không thể chứa child node).
- **Thuật toán Duyệt Cây (Recursion):** Khi GetTree hoặc Export, hệ thống xây dựng cây DTO đệ quy từ root. Đối với các tác vụ xuất bản (EPUB/Markdown export), cây canonical hoạt động như một sơ đồ duyệt (traversal path) giúp sinh mục lục phân cấp (Nested TOC) chính xác tuyệt đối.

### 5.4. Thuật Toán Xóa Phân Cấp (Topological Subtree Deletion)
- Khi thực hiện xóa một node lớn (ví dụ: Xóa một Volume hoặc toàn bộ Series), hệ thống thực thi thuật toán xóa dưới nền theo thứ tự từ dưới lên (Bottom-Up / Topological Order):
  1. Xác định toàn bộ subtree bằng cách quét đệ quy các con thuộc node chỉ định.
  2. Xóa dữ liệu payload của Chapter trước (lá).
  3. Xóa dữ liệu payload của Volume (nhánh con).
  4. Xóa dữ liệu Series (gốc).
  5. Xóa các node tương ứng trên cây `book_nodes` trong một database transaction duy nhất.

