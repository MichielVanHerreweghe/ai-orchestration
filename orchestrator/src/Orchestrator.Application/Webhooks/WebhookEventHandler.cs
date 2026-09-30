using Microsoft.Extensions.Logging;
using Orchestrator.Domain;

namespace Orchestrator.Application.Webhooks;

public sealed class WebhookEventHandler(IAgentRunner agentRunner, ILogger<WebhookEventHandler> logger)
{
    public async Task HandleAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Received GitHub event {Event} ({Action}), delivery {DeliveryId}",
            webhookEvent.Name, webhookEvent.Action, webhookEvent.DeliveryId);

        if (webhookEvent.AgentPrompt is not { } prompt)
            return;

        var agentId = await agentRunner.StartAsync(
            webhookEvent.DeliveryId, webhookEvent.Repository, webhookEvent.IssueNumber, prompt, cancellationToken);
        logger.LogInformation(
            "Requested agent {AgentId} with {Prompt} for {Repository}#{IssueNumber}",
            agentId, prompt, webhookEvent.Repository, webhookEvent.IssueNumber);
    }
}
