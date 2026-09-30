using System.Security.Cryptography;
using System.Text;

namespace Orchestrator.Infrastructure.GitHub;

public sealed class GitHubSignatureVerifier(string secret)
{
    private readonly byte[] _key = Encoding.UTF8.GetBytes(secret);

    public bool IsValid(byte[] body, string? signatureHeader)
    {
        if (string.IsNullOrEmpty(signatureHeader))
            return false;

        var expected = "sha256=" + Convert.ToHexStringLower(HMACSHA256.HashData(_key, body));
        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(expected),
            Encoding.ASCII.GetBytes(signatureHeader));
    }
}
