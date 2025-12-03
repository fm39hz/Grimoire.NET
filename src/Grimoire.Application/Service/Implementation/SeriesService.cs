namespace Grimoire.Application.Service.Implementation;

using Contract;
using Domain.Common.Repository;
using Domain.Entity.Book;
using Domain.Exception;
using Dto.Book;
using System.Text.RegularExpressions;

public sealed partial class SeriesService(ISeriesRepository repository) : ISeriesService {
	public async Task<SeriesModel?> FindOne(Guid id) => await repository.FindOne(id);

	public async Task<IEnumerable<SeriesModel>> FindAll() => await repository.FindAll();

	public async Task<SeriesModel> Create(CreateSeriesRequestDto dto) {
        var series = new SeriesModel {
            Title = dto.Title,
            Metadata = dto.Metadata,
            Slug = GenerateSlug(dto.Title)
        };
        return await repository.Create(series);
    }

	public async Task<SeriesModel> Update(Guid id, UpdateSeriesRequestDto dto) {
		var series = await repository.FindOne(id) ??
					throw new EntityNotFoundException($"Series with id {id} not found");
		series.Title = dto.Title ?? series.Title;
        if (dto.Title is not null) {
            series.Slug = GenerateSlug(dto.Title);
        }
		series.Metadata = dto.Metadata ?? series.Metadata;

		return await repository.Update(series);
	}

	public async Task<int> Delete(Guid id) => await repository.Delete(id);

    private static string GenerateSlug(string title) {
        // 1. To lower case
        var slug = title.ToLowerInvariant();

        // 2. Replace spaces and special characters with hyphens
        slug = SpaceRegex().Replace(slug, "-");
        
        // 3. Remove invalid characters
        slug = InvalidCharsRegex().Replace(slug, "");

        // 4. Trim hyphens from start and end
        slug = slug.Trim('-');

        // 5. Replace multiple hyphens with a single hyphen
        slug = HyphenRegex().Replace(slug, "-");

        return slug;
    }

    [GeneratedRegex(@"\s+", RegexOptions.Compiled)]
    private static partial Regex SpaceRegex();

    [GeneratedRegex(@"[^a-z0-9\s-]", RegexOptions.Compiled)]
    private static partial Regex InvalidCharsRegex();

    [GeneratedRegex(@"-+", RegexOptions.Compiled)]
    private static partial Regex HyphenRegex();
}
