namespace Grimoire.Application.Export;

public enum FootnoteStyle {
	/// <summary>Format as (1)</summary>
	Parentheses,

	/// <summary>Format as [1]</summary>
	SquareBrackets,

	/// <summary>Format as *, **, ***</summary>
	Asterisk,

	/// <summary>Format as superscript 1 without brackets</summary>
	SuperScript
}
