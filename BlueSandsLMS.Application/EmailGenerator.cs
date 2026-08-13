using System.Text;
using System.Text.RegularExpressions;

public static class EmailGenerator
{
    /// <summary>
    /// Generates an email address from a full name using the school domain
    /// </summary>
    /// <param name="fullName">User's full name (e.g., "John Doe")</param>
    /// <returns>Generated email address (e.g., "john.doe@school.com")</returns>
    public static string GenerateEmailFromFullName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Full name cannot be empty", nameof(fullName));

        // Parse the name into first and last parts
        var nameParts = ParseFullName(fullName);

        // Generate email: firstname.lastname@school.com
        string localPart = $"{nameParts.FirstName}.{nameParts.LastName}".ToLowerInvariant();

        // Sanitize: remove invalid characters and accents
        localPart = SanitizeLocalPart(localPart);
        localPart = RemoveAccents(localPart);

        // Remove consecutive dots
        localPart = Regex.Replace(localPart, @"\.{2,}", ".");
        localPart = localPart.Trim('.');

        // If the local part is empty, use a fallback
        if (string.IsNullOrWhiteSpace(localPart))
        {
            localPart = $"student.{Guid.NewGuid():N}".Substring(0, 20);
        }

        // Return with the school domain
        return $"{localPart}@school.com";
    }

    /// <summary>
    /// Parses a full name into first and last name parts
    /// </summary>
    private static NameParts ParseFullName(string fullName)
    {
        // Clean up the name
        var cleaned = Regex.Replace(fullName.Trim(), @"\s+", " ");
        var parts = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        return new NameParts
        {
            FirstName = parts.Length > 0 ? parts[0] : string.Empty,
            LastName = parts.Length > 1 ? parts[^1] : string.Empty
        };
    }

    /// <summary>
    /// Sanitizes the local part of the email
    /// </summary>
    private static string SanitizeLocalPart(string localPart)
    {
        // Only allow alphanumeric, dot, underscore, and hyphen
        return Regex.Replace(localPart, @"[^a-zA-Z0-9._-]", "");
    }

    /// <summary>
    /// Removes diacritics from the string
    /// </summary>
    private static string RemoveAccents(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();

        foreach (var c in normalized)
        {
            var unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    private class NameParts
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
    }
}