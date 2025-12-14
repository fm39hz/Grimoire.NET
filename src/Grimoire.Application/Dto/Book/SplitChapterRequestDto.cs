namespace Grimoire.Application.Dto.Book;

public record SplitChapterRequestDto(
	List<SplitPointDto> SplitPoints);

public record SplitPointDto(
	int SegmentIndex,
	string NewChapterTitle);
