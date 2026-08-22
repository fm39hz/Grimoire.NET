namespace Grimoire.Domain.Entity.Book.Segment;

using System.Text.Json.Serialization;

/// <summary>
///     Represents a small unit of text with formatting options.
/// </summary>
public sealed record TextRun(
	string Text,
	[property : JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	bool IsBold = false,
	[property : JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	bool IsItalic = false,
	[property : JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	string? FootnoteId = null,
	[property : JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	bool IsStrikethrough = false,
	[property : JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	bool IsHighlight = false,
	[property : JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	bool IsCode = false
	);
