using System.Text.RegularExpressions;

namespace LabelVoice.Core.Utils;

public static class VirtualPathValidator
{
    private static readonly Regex _containsBadCharacters = new("[" + Regex.Escape(new string(Path.GetInvalidFileNameChars())) + "]");

    public static bool IsValidName(string? name)
    {
        return !string.IsNullOrWhiteSpace(name) && !_containsBadCharacters.IsMatch(name);
    }

    public static bool IsValidPath(string? path)
    {
        if (path == null)
        {
            return false;
        }

        return path.Length == 0 || path.Split('/').All(IsValidName);
    }
}