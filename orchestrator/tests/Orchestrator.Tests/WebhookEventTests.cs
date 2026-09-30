using System.Text.Json;
using Orchestrator.Domain;

namespace Orchestrator.Tests;

public class WebhookEventTests
{
    [Theory]
    [InlineData("issue_comment", "created", "OWNER", "/feature-plan", false, "/feature-plan 7")]
    [InlineData("issue_comment", "created", "OWNER", "/feature-plan  Use Postgres.\nKeep it small. ", false,
        "/feature-plan 7\n\nFeedback on the current plan:\nUse Postgres.\nKeep it small.")]
    [InlineData("issue_comment", "created", "COLLABORATOR", "  /feature-implement\nplease", false, "/feature-implement")]
    [InlineData("issue_comment", "created", "OWNER", "", false, null)]
    [InlineData("issue_comment", "created", "OWNER", "/feature-planning", false, null)]
    [InlineData("issue_comment", "created", "OWNER", "please /feature-plan", false, null)]
    [InlineData("issue_comment", "created", "NONE", "/feature-plan", false, null)]
    [InlineData("issue_comment", "edited", "OWNER", "/feature-plan", false, null)]
    [InlineData("issue_comment", "created", "OWNER", "/feature-plan", true, null)]
    [InlineData("issues", "opened", "OWNER", "/feature-plan", false, null)]
    public void AgentPrompt(string eventName, string action, string author, string body, bool onPullRequest, string? expected)
    {
        // Arrange
        var issue = onPullRequest ? new { number = 7, pull_request = new { } } : (object)new { number = 7 };
        var payload = JsonSerializer.SerializeToElement(new
        {
            action,
            issue,
            comment = new { author_association = author, body },
            repository = new { full_name = "owner/repo" },
        });
        var webhookEvent = new WebhookEvent("delivery", eventName, payload);

        // Act
        var prompt = webhookEvent.AgentPrompt;

        // Assert
        Assert.Equal(expected, prompt);
    }
}
