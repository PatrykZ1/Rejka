using System.Net.Mail;

namespace backend.Application.Authentication;

public static class AccountEmail
{
    public static bool TryNormalize(string? email, out string normalizedEmail)
    {
        normalizedEmail = string.Empty;
        var trimmedEmail = email?.Trim();
        if (string.IsNullOrEmpty(trimmedEmail) || trimmedEmail.Length > 254 ||
            !MailAddress.TryCreate(trimmedEmail, out var parsedEmail) ||
            !string.Equals(parsedEmail.Address, trimmedEmail, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        normalizedEmail = trimmedEmail.ToUpperInvariant();
        return true;
    }
}