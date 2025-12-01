---
tags:
    - fm39hz/project-grimoire
    - infrastructure
---

# VOLUME IV: INFRASTRUCTURE

## 1. Storage (MinIO)

Cấu trúc Bucket:

- `grimoire-assets`: Chứa tài nguyên gốc (Covers, Content Images).
- `grimoire-exports`: Chứa thành phẩm EPUB.

## 2. Rendering Engine (Template Strategy)

Thay vì nối chuỗi thủ công (dễ lỗi), hệ thống sử dụng **RazorLight** để render HTML từ các file mẫu `.cshtml`.

### 2.1. Tech Stack

- **Library:** `RazorLight` (cho phép render Razor view từ string hoặc file mà không cần ASP.NET MVC full).

### 2.2. Templates (Ví dụ)

Chúng ta sẽ có folder `Templates/` chứa các file:

**File: `Chapter.cshtml`**

```html
@using Grimoire.Core.ViewModels
@model ChapterViewModel

<?xml version='1.0' encoding='utf-8'?>
<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml" xmlns:epub="http://www.idpf.org/2007/ops" lang="vi" xml:lang="vi">
<head>
    <title>@Model.Title</title>
    <link href="style.css" rel="stylesheet" type="text/css"/>
</head>
<body>
    <h2>@Model.Title</h2>

    <div class="long-text no-select text-justify" id="chapter-content">
        @foreach (var segment in Model.Segments)
        {
            @* --- 1. Xử lý Đoạn văn (Text) --- *@
            @if (segment is TextParagraphViewModel p)
            {
                <p>
                    @foreach (var run in p.Runs)
                    {
                        string textContent = run.Text;

                        // Xử lý Bold & Italic lồng nhau thủ công để kiểm soát chặt chẽ
                        // Lưu ý: Razor tự động encode HTML special chars trong biến textContent (an toàn)
                        if (run.IsBold && run.IsItalic)
                        {
                            textContent = $"<strong><em>{textContent}</em></strong>";
                        }
                        else if (run.IsBold)
                        {
                            textContent = $"<strong>{textContent}</strong>";
                        }
                        else if (run.IsItalic)
                        {
                            textContent = $"<em>{textContent}</em>";
                        }

                        // Xử lý Footnote Reference (Số nhỏ trên đầu)
                        if (!string.IsNullOrEmpty(run.FootnoteId))
                        {
                            // Render text trước, sau đó đến số footnote
                            // Chuẩn EPUB 3: epub:type="noteref" để Reader biết đây là link chú thích
                            @Html.Raw(textContent)<a class="footnote-link" epub:type="noteref" href="#@run.FootnoteId">[*]</a>
                        }
                        else
                        {
                            @Html.Raw(textContent)
                        }
                    }
                </p>
            }

            @* --- 2. Xử lý Ảnh Minh Họa (Image) --- *@
            else if (segment is ImageViewModel img)
            {
                <div class="img-container">
                    <img alt="@(img.Caption ?? "minh họa")" src="@img.LocalPath" />
                    @if (!string.IsNullOrEmpty(img.Caption))
                    {
                        <p class="img-caption">@img.Caption</p>
                    }
                </div>
            }

            @* --- 3. Xử lý Dòng Phân Cách (Divider) --- *@
            else if (segment is DividerViewModel)
            {
                <hr class="divider" />
            }

            @* --- 4. Xử lý Tiêu đề phụ (Heading) --- *@
            else if (segment is HeadingViewModel h)
            {
                @* Render thẻ h3, h4 tùy level *@
                @Html.Raw($"<h{h.Level}>{h.Content}</h{h.Level}>")
            }
        }
    </div>

    @* --- PHẦN RENDER NỘI DUNG FOOTNOTES (Cuối chương) --- *@
    @if (Model.Footnotes != null && Model.Footnotes.Any())
    {
        <hr class="footnote-separator" />

        @foreach (var note in Model.Footnotes)
        {
            // Chuẩn EPUB 3: epub:type="footnote" để Reader có thể hiển thị Popup thay vì nhảy trang
            <aside class="footnote-content" epub:type="footnote" id="@note.Id">
                <div class="note-header">Ghi chú:</div>

                @* Nội dung footnote cũng là một list Segment (thường là Text) *@
                @foreach (var seg in note.Segments)
                {
                    @if (seg is TextParagraphViewModel noteText)
                    {
                        <p>
                            @foreach (var run in noteText.Runs)
                            {
                                // Render text trong footnote đơn giản hơn
                                @run.Text
                            }
                        </p>
                    }
                }
            </aside>
        }
    }
</body>
</html>
```

File: `Intro.cshtml`

```html
@using Grimoire.Core.ViewModels @model IntroViewModel <?xml version='1.0'
encoding='utf-8'?>
<!DOCTYPE html>
<html
 xmlns="http://www.w3.org/1999/xhtml"
 xmlns:epub="http://www.idpf.org/2007/ops"
 lang="vi"
 xml:lang="vi"
>
 <head>
  <title>Giới thiệu</title>
  <link href="style.css" rel="stylesheet" type="text/css" />
  <style>
   /* CSS cục bộ cho trang Intro nếu cần đè style.css chung */
   .title-page {
    text-align: center;
    margin-top: 5vh;
   }
   .book-title {
    font-size: 2em;
    font-weight: bold;
    margin-bottom: 0.5em;
   }
   .book-author {
    font-size: 1.2em;
    font-style: italic;
    color: #555;
    margin-bottom: 2em;
   }
   .book-cover img {
    max-height: 50vh;
    max-width: 100%;
    box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1);
   }
   .tags {
    margin-top: 2em;
    font-size: 0.8em;
    color: #777;
   }
   .tag-item {
    display: inline-block;
    border: 1px solid #ddd;
    padding: 2px 8px;
    border-radius: 4px;
    margin: 2px;
   }

   /* Phần Front Matter */
   .front-matter {
    margin-top: 2em;
   }
   .anthology-label {
    text-align: center;
    font-weight: bold;
    text-transform: uppercase;
    letter-spacing: 2px;
    border-bottom: 1px solid #ccc;
    padding-bottom: 10px;
    margin-bottom: 20px;
   }
   .epigraph {
    margin: 3em 10%;
    font-style: italic;
    text-align: right;
    color: #444;
   }
   .section-title {
    font-weight: bold;
    margin-top: 1.5em;
    border-left: 4px solid #333;
    padding-left: 10px;
   }
  </style>
 </head>
 <body>
  @* --- PHẦN 1: TITLE PAGE --- *@
  <div class="title-page">
   <div class="book-title">@Model.BookTitle</div>
   <div class="book-author">Tác giả: @Model.Author</div>

   @if (!string.IsNullOrEmpty(Model.CoverLocalPath)) {
   <div class="book-cover">
    <img src="@Model.CoverLocalPath" alt="Cover Image" />
   </div>
   } @if (Model.Tags != null && Model.Tags.Any()) {
   <div class="tags">
    @foreach (var tag in Model.Tags) {
    <span class="tag-item">@tag</span>
    }
   </div>
   }
  </div>

  @* --- PAGE BREAK (Ngắt trang cứng) --- *@
  <div style="page-break-after: always; break-after: page;"></div>

  @* --- PHẦN 2: DETAILS (Toàn tập/Description...) --- *@
  <div class="front-matter">
   <div class="anthology-label">Toàn tập</div>

   @if (!string.IsNullOrEmpty(Model.Epigraph)) { @* Đề từ thường nằm
   riêng một khoảng *@
   <div class="epigraph">@Html.Raw(Model.Epigraph)</div>
   <hr class="divider-short" />
   } @if (!string.IsNullOrEmpty(Model.Description)) {
   <div class="section-title">Tóm tắt</div>
   <div class="description text-justify">
    @Html.Raw(Model.Description)
   </div>
   } @if (!string.IsNullOrEmpty(Model.Foreword)) {
   <div class="section-title">Lời mở đầu</div>
   <div class="foreword text-justify">@Html.Raw(Model.Foreword)</div>
   }
  </div>
 </body>
</html>
```

File: `toc.cshtml`

```html
@model List<Grimoire.Core.ViewModels.NavPointViewModel>
 <?xml version='1.0' encoding='utf-8'?>
 <!DOCTYPE html>
 <html
  xmlns="http://www.w3.org/1999/xhtml"
  xmlns:epub="http://www.idpf.org/2007/ops"
  lang="vi"
  xml:lang="vi"
 >
  <head>
   <title>Mục lục</title>
   <link href="style.css" rel="stylesheet" type="text/css" />
   <style>
    nav#toc ol {
     list-style-type: none;
     padding-left: 0;
    }
    nav#toc > ol > li {
     margin-top: 1em;
     font-weight: bold;
    } /* Volume */
    nav#toc > ol > li > ol {
     list-style-type: none;
     padding-left: 1.5em;
     font-weight: normal;
    } /* Chapter */
    nav#toc > ol > li > ol > li {
     margin-top: 0.5em;
    }
    a {
     text-decoration: none;
     color: inherit;
    }
   </style>
  </head>
  <body>
   <nav epub:type="toc" id="toc" role="doc-toc">
    <h2>Mục lục</h2>
    <ol>
     @* 1. Link tới Intro *@
     <li><a href="intro.xhtml">Lời mở đầu</a></li>

     @* 2. Link tới chính TOC *@
     <li><a href="nav.xhtml">Mục lục</a></li>

     @* 3. Vòng lặp Volume & Chapter *@ @foreach (var vol in
     Model) {
     <li>
      @if (!string.IsNullOrEmpty(vol.ContentSrc)) {
      <a href="@vol.ContentSrc">@vol.Title</a>
      } else {
      <span>@vol.Title</span>
      } @if (vol.Children != null && vol.Children.Any()) {
      <ol>
       @foreach (var chap in vol.Children) {
       <li><a href="@chap.ContentSrc">@chap.Title</a></li>
       }
      </ol>
      }
     </li>
     }
    </ol>
   </nav>
  </body>
 </html></Grimoire.Core.ViewModels.NavPointViewModel
>
```

2.3. CSS Blueprint

File styles.css chuẩn sẽ được inject vào EPUB. (Giữ nguyên CSS từ phiên bản trước).

```css
@namespace epub "" http: ; //www.idpf.org/2007/ops"";
body {
 font-family: serif;
 line-height: 1.6;
 margin: 0;
 padding: 0 5%;
}

/* Chú thích Popup */
aside[epub|type~="footnote"] {
 display: none;
} /* Ẩn nội dung gốc */
a.note-ref {
 vertical-align: super;
 font-size: 0.7em;
 text-decoration: none;
 color: inherit;
}

/* Ảnh minh họa */
.img-container {
 text-align: center;
 page-break-inside: avoid;
 margin: 1.5em 0;
}
.img-caption {
 font-size: 0.9em;
 font-style: italic;
 text-align: center;
 color: #555;
}

/* Ngắt cảnh */
hr.divider {
 border: 0;
 text-align: center;
 margin: 2em 0;
}
hr.divider::before {
 content: "* * *";
 font-weight: bold;
 letter-spacing: 0.5em;
}
```

## 3\. Docker Compose (Lean Version)

File cấu hình gọn nhẹ để chạy Local.

```yaml
version: "3.8"
name: grimoire-mvp
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

    # 2. Storage
    minio:
        image: minio/minio
        container_name: grimoire-storage
        command: server /data --console-address ":9001"
        environment:
            MINIO_ROOT_USER: admin
            MINIO_ROOT_PASSWORD: password
        volumes: [miniodata:/data]
        ports: ["9000:9000", "9001:9001"]

    # 3. API & Worker (Chạy chung container cho đơn giản hóa MVP)
    api:
        build: .
        container_name: grimoire-api
        environment:
            - ConnectionStrings__Default=Host=postgres;Database=grimoire;Username=admin;Password=admin
            - Storage__Endpoint=minio:9000
            - Storage__AccessKey=admin
            - Storage__SecretKey=password
        depends_on: [postgres, minio]
        ports: ["5000:8080"]

volumes:
    pgdata:
    miniodata:
```

## 4. Logic Điều Hướng (Navigation Strategy)

Hệ thống sử dụng cấu trúc cây `NavPoint` để tạo Mục lục phân cấp (Nested TOC) cho Anthology.

### 4.1. Cấu trúc NavPoint

```csharp
public class NavPoint {
    public string Title { get; set; }
    public string ContentSrc { get; set; } // Link tới file xhtml
    public List<NavPoint> Children { get; set; } = new();
}
```

4.2. Quy trình tạo TOC

- Root: Series Title (Cover/Intro).

- Level 1 (Volume): Tạo trang VolumeSeparator.xhtml (Chứa tên tập + Ảnh bìa tập + Tóm tắt tập).

- Level 2 (Chapter): Link tới các file nội dung chương.
