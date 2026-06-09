namespace Grimoire.Application.Dto.Book;

public record MergeChaptersRequestDto(
	List<string> ChapterIds);
