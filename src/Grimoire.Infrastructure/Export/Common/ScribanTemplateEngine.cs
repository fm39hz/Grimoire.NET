namespace Grimoire.Infrastructure.Export.Common;

using Application.Dto.Book;
using Application.Export;
using Scriban;
using Scriban.Runtime;

/// <summary>
///     Scriban implementation of the template engine
/// </summary>
public class ScribanTemplateEngine : ITemplateEngine {
	private readonly ScriptObject _sharedFunctions;

	public ScribanTemplateEngine() {
		_sharedFunctions = new ScribanExportFunctions();
		_sharedFunctions.Import("is_split_description_enabled",
			new Func<ExportSectionDto?, bool>(ExportUtilities.IsSplitDescriptionEnabled));
	}

	public string Render<T>(string templateName, T model) where T : class {
		var templateContent = GetTemplate(templateName);
		var template = Template.Parse(templateContent);

		if (template.HasErrors) {
			throw new InvalidOperationException(
				$"Template {templateName} errors: {string.Join(", ", template.Messages)}");
		}

		var context = new TemplateContext { MemberRenamer = StandardMemberRenamer.Rename };

		var modelObject = new ScriptObject();
		modelObject.Import(model, renamer: context.MemberRenamer);

		context.PushGlobal(_sharedFunctions);
		context.PushGlobal(modelObject);

		if (model is not BookExportContext exportContext) {
			return template.Render(context);
		}

		var bookUtils = new ScriptObject();
		bookUtils.Import("get_asset_url", new Func<string, string>(key =>
			exportContext.AssetFileMap.GetValueOrDefault(key, key)));
		context.PushGlobal(bookUtils);

		return template.Render(context);
	}

	private static string GetTemplate(string templateName) {
		var assembly = typeof(ScribanTemplateEngine).Assembly;
		var resourceName = $"Grimoire.Infrastructure.Export.Templates.{templateName}.scriban";

		using var stream = assembly.GetManifestResourceStream(resourceName) ??
								throw new FileNotFoundException($"Template resource {resourceName} not found.");

		using var reader = new StreamReader(stream);
		return reader.ReadToEnd();
	}
}
