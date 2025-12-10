namespace Grimoire.Application.Dto.Book;

using Service.Strategy;

public record BinderyRequestDto {
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
	///     Document structure configuration - defines the layout of the export
	///     Default: Intro -> Description -> Content -> TOC
	/// </summary>
	public ExportStructureDto Structure { get; init; } = new() {
		Sections = [
			new ExportSectionDto {
				Type = "IntroPage", Options = new Dictionary<string, object> { { "splitDescription", false } }
			},
			new ExportSectionDto { Type = "Description" },
			new ExportSectionDto { Type = "Content" },
			new ExportSectionDto { Type = "TOC" }
		]
	};
}
