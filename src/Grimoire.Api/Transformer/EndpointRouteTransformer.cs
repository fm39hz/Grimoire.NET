namespace Grimoire.Api.Transformer;

using System.Text.RegularExpressions;
using Humanizer;

public class EndpointRouteTransformer : IOutboundParameterTransformer {
	private static Regex ToKebabCaseRegex { get; } = new("([a-z])([A-Z])");

	public string? TransformOutbound(object? value) {
		if (value == null) {
			return null;
		}

		var str = value.ToString();
		var pluralized = str!.Pluralize();
		var kebabCase = ToKebabCaseRegex.Replace(pluralized, "$1-$2");
		return kebabCase.ToLowerInvariant();
	}
}
