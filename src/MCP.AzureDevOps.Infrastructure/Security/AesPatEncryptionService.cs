namespace MCP.AzureDevOps.Infrastructure.Security;

/// <summary>
/// Authenticated AES-256-GCM encryption.
/// Storage format: base64( nonce[12] + ciphertext + tag[16] ).
/// Each encryption generates a random nonce, ensuring the same PAT always
/// produces a different value in the database.
/// </summary>
internal sealed class AesPatEncryptionService : IPatEncryptionService
{
    private const int NonceSizeBytes = 12;   // 96 bits — recommended for GCM
    private const int TagSizeBytes   = 16;   // 128 bits — maximum security

    private readonly byte[] _key;

    public AesPatEncryptionService(IOptions<McpOptions> options)
    {
        var keyBase64 = options.Value.DbEncryptionKey;

        if (string.IsNullOrWhiteSpace(keyBase64))
            throw new InvalidOperationException(
                "Encryption key 'Mcp:DbEncryptionKey' is not configured. " +
                "Generate one with: openssl rand -base64 32");

        _key = Convert.FromBase64String(keyBase64);

        if (_key.Length != 32)
            throw new InvalidOperationException(
                "'Mcp:DbEncryptionKey' must be a 32-byte base64-encoded key.");
    }

    public string Encrypt(string plainText)
    {
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var nonce      = new byte[NonceSizeBytes];
        var cipher     = new byte[plainBytes.Length];
        var tag        = new byte[TagSizeBytes];

        RandomNumberGenerator.Fill(nonce);

        using var aes = new AesGcm(_key, TagSizeBytes);
        aes.Encrypt(nonce, plainBytes, cipher, tag);

        // Concatenate: nonce || ciphertext || tag
        var result = new byte[NonceSizeBytes + cipher.Length + TagSizeBytes];
        nonce.CopyTo(result, 0);
        cipher.CopyTo(result, NonceSizeBytes);
        tag.CopyTo(result, NonceSizeBytes + cipher.Length);

        return Convert.ToBase64String(result);
    }

    public string Decrypt(string cipherText)
    {
        var data   = Convert.FromBase64String(cipherText);
        var nonce  = data[..NonceSizeBytes];
        var tag    = data[^TagSizeBytes..];
        var cipher = data[NonceSizeBytes..^TagSizeBytes];
        var plain  = new byte[cipher.Length];

        using var aes = new AesGcm(_key, TagSizeBytes);
        aes.Decrypt(nonce, cipher, tag, plain);

        return Encoding.UTF8.GetString(plain);
    }
}
