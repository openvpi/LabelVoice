using System.Text.RegularExpressions;

namespace LabelVoice.Core.Utils;

public static class CodeGenerator
{
    private static readonly char[] _chars =
        { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f' };

    private static readonly HashSet<string> _registered = new();

    /// <summary>
    /// Validate the given <paramref name="code"/> with expected <paramref name="length"/>.
    /// </summary>
    /// <returns><see lanword="true"/> if <paramref name="code"/> is not null, has the given <paramref name="length"/> and is of the proper hex format, or <see langword="false"/> otherwise.</returns>
    public static bool Validate(string? code, int length)
    {
        if (code == null || code.Length != length)
        {
            return false;
        }

        return Regex.IsMatch(code, @"^[0-9a-f].*$");
    }

    /// <summary>
    /// Register a new <paramref name="code"/>.
    /// </summary>
    /// <returns><see langword="true"/> if the given <paramref name="code"/> is successfully registered, or <see langword="false"/> if the code already exists.</returns>
    public static bool Register(string code)
    {
        return _registered.Add(code);
    }

    /// <summary>
    /// Unregister a specific <paramref name="code"/>.
    /// </summary>
    public static void Unregister(string code)
    {
        _registered.Remove(code);
    }

    /// <summary>
    /// Generate and register a new unique hex code with given <paramref name="length"/>.
    /// </summary>
    /// <returns>A new code which has never been registered before.</returns>
    public static string Generate(int length)
    {
        var maxAttempts = 1 << (length * 4 - (length > 2 ? length : 0));

        for (var attempt = 0; attempt < maxAttempts; ++attempt)
        {
            var code = GenerateOnce(length);
            if (_registered.Add(code))
            {
                return code;
            }
        }

        throw new ArgumentException($"Failed to generate a unique code with this length ({length}) after {maxAttempts} attempts.", nameof(length));
    }

    private static string GenerateOnce(int length)
    {
        var codeArr = new char[length];
        var rand = new Random();
        for (var i = 0; i < length; ++i)
        {
            codeArr[i] = _chars[rand.Next(_chars.Length)];
        }
        return new string(codeArr);
    }
}
