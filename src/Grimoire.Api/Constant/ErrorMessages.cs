namespace Grimoire.Api.Constant;

public static class ErrorMessages {
	public const string InvalidSeriesIdFormat = "Invalid seriesId format";
	public const string EpubFileRequired = "EPUB file is required";
	public const string EpubFileExtension = ".epub";
	public const string MustBeEpubFile = "File must be an EPUB file (.epub)";
	public const string InvalidSeriesMetadataJson = "Invalid series metadata JSON";
	public const string InvalidVolumesMetadataJson = "Invalid volumes metadata JSON";
	public const string JobFailed = "Job failed";
	public const string ExportResultNotFound = "Export result not found or job not yet completed";
}
