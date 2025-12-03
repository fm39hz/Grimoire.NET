namespace Grimoire.Domain.Entity.Book;

/// <summary>
/// Defines the type of a chapter variant.
/// </summary>
public enum VariantType {
    /// <summary>
    /// A raw, unedited version.
    /// </summary>
    Raw = 0,
    
    /// <summary>
    /// A converted or cleaned-up version.
    /// </summary>
    Convert = 1,
    
    /// <summary>
    /// A translated version.
    /// </summary>
    Translation = 2
}
