namespace Sales.Infrastructure;

/// <summary>
/// Escapes the LIKE/ILIKE metacharacters in a user-supplied search fragment so it is matched
/// literally rather than as a pattern.
/// </summary>
/// <remarks>
/// A search term the user typed is data, not a pattern: a stray <c>%</c> or <c>_</c> must find those
/// characters, not stand in for "any sequence" or "any character". Callers append their own trailing
/// or surrounding <c>%</c> wildcards to the escaped result and pass <see cref="EscapeCharacter"/> to
/// the matching <c>Like</c>/<c>ILike</c> overload so the database honours the escaping.
/// </remarks>
internal static class LikePatternEscaper
{
    /// <summary>
    /// The escape character supplied to the <c>Like</c>/<c>ILike</c> overload. A backslash, matching
    /// what <see cref="Escape"/> prefixes each metacharacter with.
    /// </summary>
    public const string EscapeCharacter = "\\";

    /// <summary>
    /// Escapes the LIKE/ILIKE metacharacters (<c>%</c>, <c>_</c> and the escape character itself) in a
    /// search fragment.
    /// </summary>
    /// <param name="searchTerm">Raw search fragment as the user typed it.</param>
    /// <returns>The fragment with every metacharacter prefixed by <see cref="EscapeCharacter"/>.</returns>
    public static string Escape(string searchTerm)
    {
        // The escape character is replaced first: doing it after would double-escape the backslashes
        // that escaping % and _ introduces.
        return searchTerm
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
    }
}
