---
tags:
    - fm39hz/project-grimoire
    - infrastructure
---

# VOLUME IV: INFRASTRUCTURE

## 1. Storage Configuration

Hệ thống hỗ trợ cả Local Storage và S3-Compatible Object Storage (như MinIO hay AWS S3) để lưu trữ tài nguyên.

Cấu trúc lưu trữ (Bucket hoặc Thư mục gốc):
- `grimoire-assets`: Chứa tài nguyên gốc do người dùng upload lên (Covers, Content Images).
- `grimoire-exports`: Chứa thành phẩm file EPUB sau khi đóng gói.

Đường dẫn lưu trữ vật lý hoặc Object Key được chuẩn hóa theo định dạng:
`series/{SeriesId}/{FileHash}.{ext}`

---

## 2. Rendering Engine (Scriban Template Strategy)

Hệ thống sử dụng thư viện **Scriban** (`ScribanTemplateEngine`) để render các file HTML/XHTML và Markdown thành phẩm thay vì nối chuỗi thủ công hoặc sử dụng RazorLight nặng nề. Các file mẫu `.scriban` được đóng gói dưới dạng Embedded Resources trong Assembly của Infrastructure.

### 2.1. Phân Tách Trách Nhiệm Render
Để kiểm soát hiệu năng và tính chính xác của tài liệu EPUB:
1. **C# Rendering (EpubSectionRenderer.cs):** Các segment nội dung trong một Chapter (như `TextSegmentModel`, `ImageSegmentModel`, `DividerSegmentModel`, `FootnoteSegmentModel`) được duyệt qua và dựng thành chuỗi HTML thô (Raw HTML string) trực tiếp bằng C# để kiểm soát chặt chẽ thẻ đóng mở, xử lý Dropcap, và thiết lập thẻ chú thích `<aside class="footnote-inline" epub:type="footnote">`.
2. **Scriban Template Rendering:** Scriban nhận kết quả chuỗi HTML đã dựng sẵn này thông qua biến `{{ rendered_content }}` và lồng ghép vào khung sườn XHTML chuẩn EPUB 3.3.

### 2.2. Embedded Templates (.scriban)

Dưới đây là một số template Scriban tiêu biểu được khai báo trong thư mục `src/Grimoire.Infrastructure/Export/Templates`:

#### **Template: `epub_chapter.scriban`**
Dùng để dựng trang XHTML cho từng chương truyện:
```html
<?xml version='1.0' encoding='utf-8'?>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml" xmlns:epub="http://www.idpf.org/2007/ops" lang="{{ localization.language }}" xml:lang="{{ localization.language }}">
<head>
    <title>{{ title }}</title>
    <link href="style.css" rel="stylesheet" type="text/css"/>
</head>
<body>
    <h2>{{ title }}</h2>
    <div class="long-text no-select text-justify" id="chapter-content">
        {{ rendered_content }}
    </div>
</body>
</html>
```

#### **Template: `epub_volume.scriban`**
Dùng để tạo trang phân tách volume (Volume Separator Page):
```html
<?xml version='1.0' encoding='utf-8'?>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml" xmlns:epub="http://www.idpf.org/2007/ops" lang="{{ localization.language }}" xml:lang="{{ localization.language }}">
<head>
    <title>{{ title }}</title>
    <link href="style.css" rel="stylesheet" type="text/css"/>
</head>
<body>
<div class="volume-page">
    <h1 class="volume-title">{{ title }}</h1>
    {{ if cover_image_path }}
    <div class="volume-cover">
        <img src="{{ cover_image_path }}" alt="{{ title }} Cover" />
    </div>
    {{ end }}
    {{ if publication_date || isbn }}
    <div class="volume-metadata">
        {{ if publication_date }}
        <div class="metadata-item">
            <span class="metadata-label">{{ localization.publication_date_label }} </span>
            <span class="metadata-value">{{ publication_date | date.to_string "dd/MM/yyyy" }}</span>
        </div>
        {{ end }}
        {{ if isbn }}
        <div class="metadata-item">
            <span class="metadata-label">ISBN: </span>
            <span class="metadata-value">{{ isbn }}</span>
        </div>
        {{ end }}
    </div>
    {{ end }}
</div>
</body>
</html>
```

#### **Template: `epub_intro.scriban`**
Trang bìa giới thiệu (Intro / Title Page) của Series:
```html
<?xml version='1.0' encoding='utf-8'?>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml" xmlns:epub="http://www.idpf.org/2007/ops" lang="{{ localization.language }}" xml:lang="{{ localization.language }}">
<head>
    <title>{{ title }}</title>
    <link href="style.css" rel="stylesheet" type="text/css"/>
</head>
<body>
<div class="title-page">
    <div class="book-title">{{ title }}</div>
    {{ if author }}
    <div class="book-author">{{ localization.author_label }} {{ author }}</div>
    {{ end }}
    {{ if cover_local_path }}
    <div class="book-cover">
        <img src="{{ cover_local_path }}" alt="Cover Image" />
    </div>
    {{ end }}
    {{ if tags && tags.size > 0 }}
    <div class="tags">
        {{ for tag in tags }}
        <span class="tag-item">{{ tag }}</span>
        {{ end }}
    </div>
    {{ end }}
</div>

{{ if rendered_description && rendered_description.size > 0 && (section | should_show_description_in_intro) }}
<div class="front-matter">
    <div class="section-title">{{ localization.summary_label }}</div>
    <div class="description text-justify">
        {{ rendered_description }}
    </div>
</div>
{{ end }}
</body>
</html>
```

---

## 3. Docker Compose (Lean Version)

File cấu hình Docker Compose thực tế được tối ưu hóa để chạy thử nghiệm môi trường cục bộ (Local Development) một cách nhanh chóng nhất.

```yaml
name: grimoire
services:
    # 1. Database
    postgres:
        image: postgres:16-alpine
        container_name: grimoire-db
        environment:
            POSTGRES_USER: admin
            POSTGRES_PASSWORD: admin
            POSTGRES_DB: grimoire
        volumes: [pgdata:/var/lib/postgresql/data]
        ports: ["5432:5432"]

    # 2. API & Worker (Chạy chung container trong quá trình MVP Development)
    api:
        build: .
        container_name: grimoire-api
        environment:
            - ASPNETCORE_ENVIRONMENT=Development
            - OpenApi__ServerUrl=http://localhost:8080
            - ConnectionStrings__Postgre=Host=postgres;Database=grimoire;Username=admin;Password=admin
            - Storage__UseTemporaryDirectory=true
            - Storage__Type=LocalStorage
            - Storage__BasePath=grimoire-files
            - Storage__SeriesPath=series
        depends_on: [postgres]
        ports: ["8080:8080"]

volumes:
    pgdata:
    miniodata:
```

> [!TIP]
> Trong môi trường local mặc định, hệ thống cấu hình lưu file asset trên ổ đĩa vật lý của Container (`Storage__Type=LocalStorage`) và tắt dịch vụ lưu trữ Object Storage `minio` để tiết kiệm tài nguyên. Khi cần chạy S3, đổi cấu hình `Storage__Type=S3` và cung cấp cấu hình `Storage__S3__Endpoint` cùng các API key tương ứng.

---

## 4. Logic Điều Hướng & Mục Lục (Navigation)

Đóng gói file EPUB sử dụng sơ đồ cây `NavPoint` được tạo tự động khi duyệt cây canonical:
- **Root Level:** Tiêu đề của Series (Trỏ tới bìa/Intro).
- **Level 1 (Volume):** Trang ngăn cách tập `VolumeSeparator` hiển thị tiêu đề volume, ảnh bìa volume và thông tin xuất bản.
- **Level 2 (Chapter):** Điểm điều hướng trỏ tới các trang nội dung chương.
