---
tags:
    - fm39hz/project-grimoire
    - data-structure
---

# VOLUME II: DATA MODELING (MVP)

Thiết kế dữ liệu tập trung vào việc phục vụ cho Engine Render HTML/EPUB.

## 1. Relational Entities (Khung xương)

### 1.1. BookNode (Cây quản lý sách)

`book_nodes` là source of truth cho hierarchy:

- `Id` (PK): dùng chung Guid với payload entity tương ứng.
- `Type`: `Series`, `Volume`, hoặc `Chapter`.
- `ParentId`: null với `Series`; trỏ về `Series` với `Volume`; trỏ về `Volume` với `Chapter`.
- `Order`: thứ tự trong cùng parent.
- `Title`: title canonical dùng cho traversal/list/export.
- `CreatedAt`, `UpdatedAt`.

Invariant:

- Root-level persistent node chỉ được là `Series`; `BookShelf` là root logic trong DTO/API, không lưu DB.
- `Series -> Volume -> Chapter`; `Chapter` không có child.
- `(ParentId, Order)` unique để không có hai sibling cùng vị trí.

Trong phase compatibility, các bảng `Series`, `Volume`, `Chapter` vẫn giữ cột `Title`, `Order`, `SeriesId`, `VolumeId` để payload/API cũ hoạt động. Write path mới phải đi qua `IBookTreeService` và mirror các cột legacy này.

### 1.2. Series (Bộ truyện)

- `Id` (PK), `Title`.
- `Metadata` (JSONB): Chứa thông tin cấp độ Bộ (Tác giả gốc, Tóm tắt chung, Ảnh bìa bộ).
- Hierarchy lấy từ `book_nodes`, không tự xem `Volumes` là source of truth.

### 1.3. Volume (Tập)

- `Id` (PK), `SeriesId` (FK), `Order` (Số thứ tự).
- `Title` (Tên tập, vd: "Tập 7.5").
- `Metadata` (JSONB): Chứa thông tin cấp độ Tập (Họa sĩ minh họa tập này, Ảnh bìa tập).
- `SeriesId`, `Order`, `Title` được mirror từ node để giữ API cũ.

### 1.4. Chapter (Chương)

- `Id` (PK), `VolumeId` (FK), `Order`.
- `Title` (Tên chương).
- `Content` (JSONB): Chứa danh sách `Segment`.
- `VolumeId`, `Order`, `Title` được mirror từ node để giữ API cũ.

### 1.5. Assets (Quản lý File)

Bảng này tracking file vật lý và ownership logic trên tree.

- `Id` (PK)
- `SeriesId` (FK) - legacy scope/compatibility, vẫn giữ để API cũ và delete theo series hoạt động trong phase đầu.
- `OwnerNodeId` (FK nullable -> `book_nodes.Id`) - node sở hữu canonical của asset. `null` nghĩa là asset đã được nâng lên root logic `BookShelf`.
- `Path` (Key) - Vd: `series/{seriesId}/{hash}.jpg` hoặc staging/import key.
- `FileHash` (String) - SHA256 của file ảnh. Upload mới dedupe toàn cục theo hash trước khi tạo row mới.
- `RefType` - "Cover" hoặc "Content".

Ownership được reconcile sau import/sync bằng cách scan reference trong series metadata, volume metadata, và chapter image segments. Nếu cùng một asset được dùng ở nhiều node, `OwnerNodeId` trở thành lowest common ancestor của các node đó: chapter + volume cover -> volume; hai volume khác nhau -> series; nhiều series -> `null`/`BookShelf`.

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
