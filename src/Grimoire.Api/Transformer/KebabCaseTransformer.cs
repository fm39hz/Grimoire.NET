namespace Grimoire.Api.Transformer;

using System.Text.RegularExpressions;

public partial class KebabCaseTransformer : IOutboundParameterTransformer {
	public string? TransformOutbound(object? value) =>
		value == null? null : ToSnakeCaseRegex().Replace(value.ToString()!, "$1-$2").ToLowerInvariant();

	[GeneratedRegex("([a-z])([A-Z])")]
	private static partial Regex ToSnakeCaseRegex();
}
