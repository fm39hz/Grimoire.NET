namespace Grimoire.Application.Publish.Import.Steps;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grimoire.Application.Dto.Book;
using Grimoire.Application.Dto.Book.Metadata;
using Grimoire.Application.Dto.Book.Segment;
using Grimoire.Application.Import;
using Grimoire.Application.Mapper;
using Grimoire.Application.Service.Contract;
using Grimoire.Domain.Entity.Book.Segment;

public sealed class MetadataResolutionStep(
    ISeriesService seriesService,
    IXhtmlSegmentParser xhtmlParser,
    IBookMapper mapper) : IImportPipelineStep
{
    public int Order => 20;

    public async Task ExecuteAsync(ImportPipelineContext context, CancellationToken cancellationToken)
    {
        var title = context.SeriesDto?.Title;
        if (string.IsNullOrWhiteSpace(title) && context.Normalized is not null)
        {
            title = context.Normalized.Title;
        }
        if (string.IsNullOrWhiteSpace(title))
        {
            title = "Untitled";
        }

        var authors = context.SeriesDto?.Metadata?.Authors;
        if ((authors == null || authors.Count == 0) && context.Normalized is not null && !string.IsNullOrWhiteSpace(context.Normalized.Author))
        {
            authors = new List<string> { context.Normalized.Author };
        }

        var description = context.SeriesDto?.Metadata?.Description;
        if ((description == null || description.Count == 0) && context.Normalized is not null && !string.IsNullOrWhiteSpace(context.Normalized.Description))
        {
            var parsed = xhtmlParser.Parse(context.Normalized.Description, new Dictionary<string, byte[]>());
            description = parsed.Segments
                .OfType<TextSegmentModel>()
                .Select(mapper.ToTextSegmentDto)
                .ToList();
        }

        var tags = context.SeriesDto?.Metadata?.Tags;
        if ((tags == null || tags.Count == 0) && context.Normalized is not null)
        {
            tags = context.Normalized.Tags;
        }

        var coverImage = context.SeriesDto?.Metadata?.CoverImage;

        context.SeriesDto = new CreateSeriesRequestDto(
            title,
            new SeriesMetadataDto
            {
                Authors = authors ?? new List<string>(),
                Artists = context.SeriesDto?.Metadata?.Artists ?? new List<string>(),
                Tags = tags ?? new List<string>(),
                Description = description ?? new List<TextSegmentDto>(),
                CoverImage = coverImage
            }
        );

        var (series, _) = await seriesService.GetOrCreate(context.SeriesDto, cancellationToken);
        context.Series = series;
        context.ReportSubProgress(1.0);
    }
}
