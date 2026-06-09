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
- `Order` (float): thứ tự trong cùng parent (hỗ trợ số thực/fractional để dễ dàng chèn node ở giữa mà không cần tính toán lại toàn bộ cây).
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
- `FileHash` (String) - SHA256 hoặc MD5 của file ảnh. Upload mới dedupe toàn cục theo hash trước khi tạo row mới.
- `RefType` - "Cover" hoặc "Content".
- `ContentType` - MIME type của file khi upload (e.g. image/jpeg, image/png).
- `OriginalFileName` - Tên file gốc lúc upload.

Ownership được reconcile sau import/sync bằng cách scan reference trong series metadata, volume metadata, và chapter image segments. Nếu cùng một asset được dùng ở nhiều node, `OwnerNodeId` trở thành lowest common ancestor của các node đó: chapter + volume cover -> volume; hai volume khác nhau -> series; nhiều series -> `null`/`BookShelf`.

## 2. JSONB Structures (Nội dung)

### 2.1. Metadata Objects

Lưu trữ thông tin linh hoạt, không cần Migration khi thêm trường mới. Có hai cấu trúc riêng cho Series và Volume:

**Series Metadata:**
```csharp
public sealed record SeriesMetadata {
	public ICollection<string> Authors { get; init; } = [];
	public ICollection<string> Artists { get; init; } = [];
	public ICollection<string> Tags { get; init; } = [];
	public List<TextSegmentModel> Description { get; init; } = [];
	public string CoverImage { get; init; } = string.Empty;
}
```

**Volume Metadata:**
```csharp
public sealed record VolumeMetadata {
	public string? CoverImage { get; init; } = string.Empty;
	public DateTime? PublicationDate { get; init; }
	public string Isbn { get; init; } = string.Empty;
}
```

### 2.2. Content Segments

Chỉ lưu những gì cần thiết để hiển thị (Render).

```csharp
// Đơn vị Text nhỏ nhất
public sealed record TextRun(
	string Text,
	[property : JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	bool IsBold = false,
	[property : JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	bool IsItalic = false,
	[property : JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	string? FootnoteId = null
);

// Segment Base
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(TextSegmentModel), "Text")]
[JsonDerivedType(typeof(ImageSegmentModel), "Image")]
[JsonDerivedType(typeof(DividerSegmentModel), "Divider")]
[JsonDerivedType(typeof(FootnoteSegmentModel), "Footnote")]
public abstract record SegmentModel {
	public Guid Id { get; init; } = Guid.CreateVersion7();
}

// 1. Text Segment
public sealed record TextSegmentModel : SegmentModel {
	public List<TextRun> Runs { get; init; } = [];
}

// 2. Image Segment
public sealed record ImageSegmentModel : SegmentModel {
	public required string AssetKey { get; init; }
	public string? Caption { get; init; }
}

// 3. Divider Segment
public sealed record DividerSegmentModel : SegmentModel {
	public string Style { get; init; } = "* * *";
}

// 4. Footnote Segment
public record FootnoteSegmentModel : SegmentModel {
	public List<TextSegmentModel> Segments { get; init; } = [];
}
```
