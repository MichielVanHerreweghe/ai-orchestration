using System.Text;
using Orchestrator.Infrastructure.GitHub;

namespace Orchestrator.Tests;

public class GitHubSignatureVerifierTests
{
    // Example from https://docs.github.com/en/webhooks/using-webhooks/validating-webhook-deliveries
    private const string Secret = "It's a Secret to Everybody";
    private static readonly byte[] Body = Encoding.UTF8.GetBytes("Hello, World!");
    private const string Signature = "sha256=757107ea0eb2509fc211221cce984b8a37570b6d7586c22c46f4379c8b043e17";

    [Theory]
    [InlineData(Signature, true)]
    [InlineData("sha256=0000000000000000000000000000000000000000000000000000000000000000", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValid(string? header, bool expected)
    {
        var verifier = new GitHubSignatureVerifier(Secret);

        var result = verifier.IsValid(Body, header);

        Assert.Equal(expected, result);
    }
}
