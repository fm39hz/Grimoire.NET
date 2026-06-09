namespace Grimoire.Application.Export;

public enum FootnoteMode {
	/// <summary>Footnotes rendered inline at the end of each chapter (Kindle-compatible popup).</summary>
	Inline,

	/// <summary>Footnotes consolidated into a separate endnotes page per volume.</summary>
	PerVolume,

	/// <summary>All footnotes consolidated into a single endnotes page for the entire book.</summary>
	Global
}
