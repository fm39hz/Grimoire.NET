namespace Grimoire.Application.Dto.Book;

using Service.Strategy;

public record ExportSeriesRequestDto {
	/// <summary>
	///     Export format (Epub, Pdf, Mobi, Html)
	/// </summary>
	public ExportFormat Format { get; init; } = ExportFormat.Epub;

	/// <summary>
	///     Export mode (e.g., "Anthology" for all volumes, "Single" for specific volumes)
	/// </summary>
	public string Mode { get; init; } = "Anthology";

	/// <summary>
	///     List of volume IDs to export (optional, used when Mode is "Single")
	/// </summary>
	public List<string>? TargetVolumeIds { get; init; }

	/// <summary>
	///     Whether to inject CSS into the export
	/// </summary>
	public bool InjectCss { get; init; } = true;
}
