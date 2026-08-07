namespace Grimoire.Application.Service.Contract;

using Dto.Book.Restructure;
using Dto.Book.Tree;

public interface IBookRestructureService {
	/// <summary>
	///     Executes an ordered batch of atomic restructuring operations on a series, in a single
	///     transaction. Server validates and interprets each op; if any fails, the whole batch
	///     rolls back. Returns the resulting book tree.
	/// </summary>
	public Task<BookTreeDto> ExecuteAsync(BookRestructureRequestDto request, CancellationToken cancellationToken = default);
}
