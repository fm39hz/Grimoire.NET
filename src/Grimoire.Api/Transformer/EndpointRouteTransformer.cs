namespace Grimoire.Api.Transformer;

using System.Text.RegularExpressions;
using Humanizer;

public partial class EndpointRouteTransformer : IOutboundParameterTransformer {
	[GeneratedRegex("([a-z])([A-Z])")]
	private static partial Regex ToKebabCaseRegex();

	public string? TransformOutbound(object? value) {
		if (value == null) {
			return null;
		}

		var str = value.ToString();
		var pluralized = str!.Pluralize();
		var kebabCase = ToKebabCaseRegex().Replace(pluralized, "$1-$2");
		return kebabCase.ToLowerInvariant();
	}
}
