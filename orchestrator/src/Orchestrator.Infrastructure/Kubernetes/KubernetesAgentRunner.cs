using System.Net;
using k8s.Autorest;
using k8s.Models;
using KubeOps.KubernetesClient;
using Orchestrator.Application.Webhooks;

namespace Orchestrator.Infrastructure.Kubernetes;

/// <summary>Records the run as an <see cref="V1AgentRun"/>; <see cref="AgentRunController"/> starts it.</summary>
public sealed class KubernetesAgentRunner(IKubernetesClient client) : IAgentRunner
{
    public async Task<string> StartAsync(
        string deliveryId, string repository, int issueNumber, string prompt, CancellationToken cancellationToken)
    {
        var run = new V1AgentRun
        {
            // Named after the delivery, so a redelivered webhook can't start a second run.
            Metadata = new V1ObjectMeta
            {
                Name = $"agent-{deliveryId.ToLowerInvariant()}",
                NamespaceProperty = await client.GetCurrentNamespaceAsync(cancellationToken: cancellationToken),
            },
            Spec = new V1AgentRun.EntitySpec
            {
                Repository = repository,
                Issue = issueNumber,
                Command = prompt.Split()[0],
                Prompt = prompt,
            },
        };

        try
        {
            await client.CreateAsync(run, cancellationToken);
        }
        catch (HttpOperationException e) when (e.Response.StatusCode == HttpStatusCode.Conflict)
        {
            // A redelivery: the run already exists.
        }

        return run.Name();
    }
}
