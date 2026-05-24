using System.Diagnostics;
using System.Text.Json.Serialization;

namespace MCP.AzureDevOps.Domain.ValueObjects;

/// <summary>
/// Value object que encapsula un Personal Access Token de Azure DevOps.
/// El valor se protege explícitamente contra serialización y visualización accidental:
/// <list type="bullet">
///   <item><see cref="JsonIgnoreAttribute"/> impide que <c>Value</c> aparezca en JSON.</item>
///   <item><see cref="DebuggerBrowsableAttribute"/> oculta el valor en el depurador.</item>
///   <item>No sobreescribe <c>ToString()</c> para evitar logging accidental.</item>
/// </list>
/// </summary>
[DebuggerDisplay("PersonalAccessToken(***REDACTED***)")]
public sealed record PersonalAccessToken
{
    [JsonIgnore]
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public string Value { get; }

    public PersonalAccessToken(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("PersonalAccessToken cannot be empty.", nameof(value));
        Value = value.Trim();
    }

    // Sin ToString() — evita logging accidental del token en trazas y mensajes de excepción
}
