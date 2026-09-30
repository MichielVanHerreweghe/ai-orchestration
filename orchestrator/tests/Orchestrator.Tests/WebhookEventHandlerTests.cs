using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Orchestrator.Application.Webhooks;
using Orchestrator.Domain;

namespace Orchestrator.Tests;

public class WebhookEventHandlerTests
{
    private sealed class FakeAgentRunner : IAgentRunner
    {
        public List<(string Repository, int IssueNumber, string Prompt)> Started { get; } = [];

        public Task<string> StartAsync(
            string deliveryId, string repository, int issueNumber, string prompt, CancellationToken cancellationToken)
        {
            Started.Add((repository, issueNumber, prompt));
            return Task.FromResult("agent-id");
        }
    }

    [Fact]
    public async Task CommandComment_StartsAgentForIssue()
    {
        // Arrange
        var runner = new FakeAgentRunner();
        var handler = new WebhookEventHandler(runner, NullLogger<WebhookEventHandler>.Instance);
        var payload = JsonDocument.Parse("""
            {"action":"created","issue":{"number":7},"comment":{"author_association":"OWNER","body":"/feature-plan"},
             "repository":{"full_name":"owner/repo"}}
            """).RootElement;

        // Act
        await handler.HandleAsync(new WebhookEvent("delivery", "issue_comment", payload), CancellationToken.None);

        // Assert
        Assert.Equal([("owner/repo", 7, "/feature-plan 7")], runner.Started);
    }
}
