namespace Orchestrator.Application.Webhooks;

public interface IAgentRunner
{
    /// <summary>
    /// Starts an agent on an issue of <paramref name="repository"/> (owner/name) and returns its id.
    /// Starting again for the same <paramref name="deliveryId"/> starts nothing new.
    /// </summary>
    Task<string> StartAsync(
        string deliveryId, string repository, int issueNumber, string prompt, CancellationToken cancellationToken);
}
