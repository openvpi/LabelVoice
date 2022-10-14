using System.Text.RegularExpressions;

namespace LabelVoice.Core.Utils;

public static class VirtualPathValidator
{
    private static readonly Regex _noSurroundingWhitespaces = new(@"^(?!\s).*(?!\s)$");

    private static readonly Regex _containsBadCharacters = new("^(?!\\s)[" + Regex.Escape(new string(Path.GetInvalidFileNameChars())) + "]");

    /// <summary>
    /// Tell if the given <paramref name="name"/> is valid for items and slices.<br/>
    /// A valid name is not null, does not start or end with whitespaces, and does not contain any characters in <see cref="Path.GetInvalidFileNameChars()"/>.
    /// </summary>
    public static bool IsValidName(string? name)
    {
        return name != null && _noSurroundingWhitespaces.IsMatch(name) && !_containsBadCharacters.IsMatch(name);
    }

    /// <summary>
    /// Tell if the given <paramref name="path"/> is valid for items and placeholders.<br/>
    /// A valid path is not null, and is either an empty string or a string joined with valid name(s) with the '/' separator.
    /// </summary>
    public static bool IsValidPath(string? path)
    {
        return path != null && (path.Length == 0 || path.Split('/').All(IsValidName));
    }
}
