namespace Grimoire.Infrastructure.Export.Common;

/// <summary>
///     Interface for template rendering engine
/// </summary>
public interface ITemplateEngine {
	/// <summary>
	///     Renders a template with the provided model asynchronously
	/// </summary>
	/// <typeparam name="T">Model type</typeparam>
	/// <param name="templateName">Name of the template (without extension)</param>
	/// <param name="model">Model data</param>
	/// <returns>Rendered string</returns>
	public string Render<T>(string templateName, T model) where T : class;
}
