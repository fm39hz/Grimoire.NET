namespace Grimoire.Application.Dto.Book;

using System;

public record IngestionAuditResponseDto(
	string Id,
	string SeriesId,
	string SourceType,
	string Status,
	string? ErrorMessage,
	string? Summary,
	DateTimeOffset StartedAt,
	DateTimeOffset? CompletedAt);
