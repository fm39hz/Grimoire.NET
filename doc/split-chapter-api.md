# Split Chapter API

## Endpoint

```
POST /api/v1/chapter/{id}/split
```

## Description

Splits a chapter into multiple chapters at specified segment boundaries. The original chapter is updated to contain only the segments before the first split point, and new chapters are created for the remaining segments.

## Request

### Path Parameters

- `id` (string, required): The prefixed ID of the chapter to split (format: `chap_...`)

### Body (JSON)

```json
{
  "splitPoints": [
    {
      "segmentIndex": 25,
      "newChapterTitle": "Chapter 1, Part 2"
    },
    {
      "segmentIndex": 50,
      "newChapterTitle": "Chapter 1, Part 3"
    }
  ]
}
```

#### Fields

- `splitPoints` (array, required): List of split points where the chapter will be divided
  - `segmentIndex` (integer, required): Index in the segments array where a new chapter begins (must be > 0 and < total segments)
  - `newChapterTitle` (string, required): Title for the new chapter starting at this point (max 500 characters)

#### Validation Rules

1. At least one split point is required
2. Segment indices must be strictly increasing (no duplicates)
3. Segment indices must be within the bounds of the chapter's segments
4. Each new chapter title is required and cannot be empty

## Response

### Success (201 Created)

Returns an array of all resulting chapters (original + newly created) in order:

```json
[
  {
    "id": "chap_01h2xz9...",
    "volumeId": "vol_01h2xy8...",
    "title": "Chapter 1, Part 1",
    "order": 1.0,
    "status": "Draft",
    "content": {
      "segments": [...],
      "footnotes": [...]
    }
  },
  {
    "id": "chap_01h2xza...",
    "volumeId": "vol_01h2xy8...",
    "title": "Chapter 1, Part 2",
    "order": 1.1,
    "status": "Draft",
    "content": {
      "segments": [...],
      "footnotes": [...]
    }
  },
  {
    "id": "chap_01h2xzb...",
    "volumeId": "vol_01h2xy8...",
    "title": "Chapter 1, Part 3",
    "order": 1.2,
    "status": "Draft",
    "content": {
      "segments": [...],
      "footnotes": [...]
    }
  }
]
```

### Error Responses

#### 404 Not Found
Chapter with the specified ID does not exist.

#### 400 Bad Request
- Invalid split points (out of bounds, not strictly increasing, etc.)
- Chapter has no content
- Validation errors

## Behavior

1. **Original Chapter**: Updated to contain segments from index 0 to `splitPoints[0].segmentIndex - 1`
2. **New Chapters**: Created for each split point with:
   - Segments from the split point to the next split point (or end of chapter)
   - Fractional ordering (original.order + 0.1, 0.2, etc.)
   - Same volume and status as the original chapter
3. **Footnotes**: Automatically redistributed based on which segments reference them
4. **Transaction**: All changes occur atomically (all succeed or all fail)

## Example

Given a chapter with 75 segments, splitting at indices 25 and 50:

```bash
curl -X POST http://localhost:5000/api/v1/chapter/chap_01h2xz9.../split \
  -H "Content-Type: application/json" \
  -d '{
    "splitPoints": [
      { "segmentIndex": 25, "newChapterTitle": "Part II" },
      { "segmentIndex": 50, "newChapterTitle": "Part III" }
    ]
  }'
```

Result:
- Original chapter: segments 0-24
- New chapter 1: segments 25-49 (title: "Part II")
- New chapter 2: segments 50-74 (title: "Part III")

## Implementation Details

- Location: `src/Grimoire.Api/Controller/ChapterController.cs:68`
- Service: `src/Grimoire.Application/Service/Implementation/ChapterService.cs:78`
- DTO: `src/Grimoire.Application/Dto/Book/SplitChapterRequestDto.cs`
- Validator: `src/Grimoire.Application/Dto/Book/Validators/SplitChapterRequestDtoValidator.cs`
