namespace Orchestrator.Infrastructure.Kubernetes;

/// <param name="Image">The agent image each run's Job starts.</param>
/// <param name="SecretName">The Secret whose keys become the agent's environment (CLAUDE_CODE_OAUTH_TOKEN, GH_TOKEN).</param>
public sealed record AgentSettings(string Image, string SecretName);
