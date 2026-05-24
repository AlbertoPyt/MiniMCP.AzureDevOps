namespace MCP.AzureDevOps.Infrastructure.Security;

/// <summary>
/// Cifrado/descifrado de Personal Access Tokens antes de persistirlos en base de datos.
/// </summary>
internal interface IPatEncryptionService
{
    /// <summary>Cifra un PAT en texto plano y devuelve el token cifrado en base64.</summary>
    string Encrypt(string plainText);

    /// <summary>Descifra un token cifrado en base64 y devuelve el PAT en texto plano.</summary>
    string Decrypt(string cipherText);
}
