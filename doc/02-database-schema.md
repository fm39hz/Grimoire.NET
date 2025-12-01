---
tags:
    - fm39hz/project-grimoire
    - data-structure
---

# VOLUME II: DATA MODELING (MVP)

Thiết kế dữ liệu tập trung vào việc phục vụ cho Engine Render HTML/EPUB.

## 1. Relational Entities (Khung xương)

### 1.1. Series (Bộ truyện)

- `Id` (PK), `Title`.
- `Metadata` (JSONB): Chứa thông tin cấp độ Bộ (Tác giả gốc, Tóm tắt chung, Ảnh bìa bộ).
- `Volumes` (Collection).

### 1.2. Volume (Tập)

- `Id` (PK), `SeriesId` (FK), `Order` (Số thứ tự).
- `Title` (Tên tập, vd: "Tập 7.5").
- `Metadata` (JSONB): Chứa thông tin cấp độ Tập (Họa sĩ minh họa tập này, Ảnh bìa tập).

### 1.3. Chapter (Chương)

- `Id` (PK), `VolumeId` (FK), `Order`.
- `Title` (Tên chương).
- `Content` (JSONB): Chứa danh sách `Segment`.

### 1.4. Assets (Quản lý File)

Bảng này giúp tracking file nào đang được sử dụng, tránh rác trong MinIO.

- `Id` (PK)
- `SeriesId` (FK) - Để khi xóa Series thì xóa luôn ảnh thuộc về nó.
- `MinioPath` (Key) - Vd: "covers/uuid.jpg"
- `FileHash` (String) - MD5/SHA256 của file ảnh (Dùng để check trùng).
- `RefType` - "Cover" hoặc "Content".

## 2. JSONB Structures (Nội dung)

### 2.1. Metadata Object

Lưu trữ thông tin linh hoạt, không cần Migration khi thêm trường mới.

```csharp
public record BookMetadata
{
    public List<string> Authors { get; init; } = new();
    public List<string> Artists { get; init; } = new();
    public List<string> Tags { get; init; } = new();
    public string? Description { get; init; }   // HTML Supported
    public string? CoverAssetKey { get; init; } // MinIO Path
    public string Language { get; init; } = "vi";
}
```

### 2.2. Content Segments (Đơn giản hóa)

Chỉ lưu những gì cần thiết để hiển thị (Render).

```csharp
// Đơn vị Text nhỏ nhất
public record TextRun(
    string Text,
    [property: JsonIgnore(Condition = WhenWritingDefault)] bool IsBold = false,
    [property: JsonIgnore(Condition = WhenWritingDefault)] bool IsItalic = false,
    [property: JsonIgnore(Condition = WhenWritingNull)] string? FootnoteId = null
);

// Segment Base
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(TextSegment), "text")]
[JsonDerivedType(typeof(ImageSegment), "image")]
[JsonDerivedType(typeof(DividerSegment), "divider")]
public abstract record SegmentBase { public Guid Id { get; init; } = Guid.NewGuid(); }

// 1. Text Segment
public record TextSegment : SegmentBase
{
    public List<TextRun> Runs { get; init; } = new();
}

// 2. Image Segment
public record ImageSegment : SegmentBase
{
    public string AssetKey { get; init; }
    public string? Caption { get; init; }
}

// 3. Divider Segment
public record DividerSegment : SegmentBase
{
    public string Style { get; init; } = "* * *";
}
```
