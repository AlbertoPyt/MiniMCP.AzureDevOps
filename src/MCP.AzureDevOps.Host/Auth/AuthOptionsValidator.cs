namespace MCP.AzureDevOps.Host.Auth;

/// <summary>
/// Validates <see cref="AuthOptions"/> at application startup.
/// An empty key is allowed (development mode without authentication), but if
/// configured it must have at least <see cref="MinKeyLength"/> characters to
/// prevent trivially weak keys.
/// </summary>
internal sealed class AuthOptionsValidator : IValidateOptions<AuthOptions>
{
    private const int MinKeyLength = 16;

    public ValidateOptionsResult Validate(string? name, AuthOptions options)
    {
        // Empty key is allowed — the handler emits a warning at runtime
        if (string.IsNullOrWhiteSpace(options.AdminApiKey))
            return ValidateOptionsResult.Success;

        if (options.AdminApiKey.Length < MinKeyLength)
            return ValidateOptionsResult.Fail(
                $"'Auth:AdminApiKey' must be at least {MinKeyLength} characters to avoid weak keys. " +
                "Generate a key with: openssl rand -base64 32");

        return ValidateOptionsResult.Success;
    }
}
