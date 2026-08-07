namespace Grimoire.Application.Dto.Book.Restructure;

/// <summary>
///     A batch of atomic restructuring operations on one series. Executed in order, in a single
///     transaction — if any op fails, the whole batch rolls back.
/// </summary>
public record BookRestructureRequestDto(
	string SeriesId,
	List<BookRestructureOp> Operations);
