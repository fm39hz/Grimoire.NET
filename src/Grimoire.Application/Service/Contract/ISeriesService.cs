namespace Grimoire.Application.Service.Contract;

using System.Threading;
using Domain.Common;
using Domain.Entity.Book;
using Dto.Book;
using Dto.Common;

public interface ISeriesService : ICrudService<SeriesModel, CreateSeriesRequestDto, UpdateSeriesRequestDto> {
	public Task<(SeriesModel Series, bool Created)> GetOrCreate(CreateSeriesRequestDto dto, CancellationToken cancellationToken = default);
	public Task<IEnumerable<VolumeModel>> FindAllVolumes(Guid seriesId, CancellationToken cancellationToken = default);
	public Task<PagedResult<VolumeModel>> FindAllVolumes(Guid seriesId, PaginationRequest pagination, CancellationToken cancellationToken = default);

	// TODO: Implement Trigram-based fuzzy search with typo tolerance and phonetic matching (Double Metaphone) for Series and Glossary Terms.
	// We can leverage:
	// - EF.Functions.TrigramsAreSimilar(title, query) to filter terms
	// - EF.Functions.TrigramsSimilarity(title, query) to rank autocomplete results by similarity
	// - EF.Functions.FuzzyStringMatchDoubleMetaphone(term) for phonetic matching of fantasy names
}
