namespace Grimoire.Api.Extension;

using Domain.Entity.Book;
using Microsoft.AspNetCore.Mvc.ModelBinding;

/// <summary>
///     Tells MVC's model-validation walker to skip the Npgsql <see cref="LTree"/> path members
///     (<c>Path</c> / <c>DbPath</c>) on segment models. The <see cref="LTree"/> type's
///     <c>NLevel</c> getter has no validation-safe translation, so visiting it during model
///     binding throws. The path is derived state, not client input, and must never be bound.
/// </summary>
public sealed class LTreeValidationMetadataProvider : SuppressChildValidationMetadataProvider {
	public LTreeValidationMetadataProvider()
		: base(typeof(SegmentModel)) {
	}
}