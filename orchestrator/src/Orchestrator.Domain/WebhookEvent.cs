using System.Text.Json;

namespace Orchestrator.Domain;

public sealed record WebhookEvent(string DeliveryId, string Name, JsonElement Payload)
{
    // Anyone can comment on a public repo; only these may start an agent.
    private static readonly string[] TrustedAuthors = ["OWNER", "MEMBER", "COLLABORATOR"];

    // Each maps (issue number, text after the command) to the agent prompt.
    private static readonly Dictionary<string, Func<int, string, string>> Commands = new()
    {
        ["/feature-plan"] = (issueNumber, feedback) => feedback.Length == 0
            ? $"/feature-plan {issueNumber}"
            : $"/feature-plan {issueNumber}\n\nFeedback on the current plan:\n{feedback}",
        // Takes no issue number: the agent runs it on the issue's feature branch, which holds the plan.
        ["/feature-implement"] = (_, _) => "/feature-implement",
    };

    public string? Action => GetString(Payload, "action");

    public string Repository => Payload.GetProperty("repository").GetProperty("full_name").GetString()!;

    public int IssueNumber => Payload.GetProperty("issue").GetProperty("number").GetInt32();

    /// <summary>
    /// The agent prompt for a slash command opening a new issue comment by a trusted author; otherwise null.
    /// Text after the command is passed on as feedback.
    /// </summary>
    public string? AgentPrompt
    {
        get
        {
            if (Name != "issue_comment" || Action != "created"
                || !Payload.TryGetProperty("issue", out var issue) || issue.TryGetProperty("pull_request", out _)
                || !Payload.TryGetProperty("comment", out var comment)
                || !TrustedAuthors.Contains(GetString(comment, "author_association")))
                return null;

            var parts = (GetString(comment, "body") ?? "").Trim().Split((char[]?)null, 2);
            return Commands.TryGetValue(parts[0], out var prompt)
                ? prompt(IssueNumber, parts.Length > 1 ? parts[1].Trim() : "")
                : null;
        }
    }

    private static string? GetString(JsonElement element, string property) =>
        element.ValueKind == JsonValueKind.Object
        && element.TryGetProperty(property, out var value)
        && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}
