namespace Grimoire.Infrastructure.Export.Epub;

/// <summary>
///     HTML manipulation helpers
/// </summary>
public static class HtmlHelper {
	/// <summary>
	///     Injects custom CSS into an HTML document
	/// </summary>
	public static string InjectCustomCss(string html, string customCss) {
		var headCloseIndex = html.IndexOf("</head>", StringComparison.OrdinalIgnoreCase);
		if (headCloseIndex > 0) {
			var styleTag = $"\n<style>\n{customCss}\n</style>\n";
			return html.Insert(headCloseIndex, styleTag);
		}

		return html;
	}
}
