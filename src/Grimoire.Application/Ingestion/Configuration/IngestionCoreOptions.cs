namespace Grimoire.Application.Ingestion.Configuration;

/// <summary>
///     Rollout switches for the source-package reconciliation core.
///     All switches default to false so deploying the foundation does not change legacy behavior.
/// </summary>
public sealed class IngestionCoreOptions {
	public const string SectionName = "Features:IngestionCore";

	/// <summary>Expose the new import-run API.</summary>
	public bool Enabled { get; init; }

	/// <summary>Analyze legacy imports without using the generated plan to mutate the book tree.</summary>
	public bool ShadowMode { get; init; }

	/// <summary>Allow analysis to call configured external research providers.</summary>
	public bool ResearchEnabled { get; init; }

	/// <summary>Route legacy EPUB and series-sync requests through source-package adapters.</summary>
	public bool LegacyAdaptersEnabled { get; init; }
}
