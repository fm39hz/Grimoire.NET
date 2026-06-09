namespace Grimoire.Infrastructure.Configuration;

public class StorageConfiguration {
	public const string SECTION_NAME = "Storage";

	public string Type { get; set; } = "LocalStorage";
	public string BasePath { get; set; } = "grimoire-files";
	public string SeriesPath { get; set; } = "series";
	public bool UseTemporaryDirectory { get; set; } = true;
}
