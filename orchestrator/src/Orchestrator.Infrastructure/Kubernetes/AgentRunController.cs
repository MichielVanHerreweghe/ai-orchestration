using System.Net;
using System.Text.Json;
using k8s.Autorest;
using k8s.Models;
using KubeOps.Abstractions.Entities;
using KubeOps.Abstractions.Reconciliation;
using KubeOps.Abstractions.Reconciliation.Controller;
using KubeOps.KubernetesClient;
using Microsoft.Extensions.Logging;
using Phases = Orchestrator.Infrastructure.Kubernetes.V1AgentRun.Phases;
using Result = KubeOps.Abstractions.Reconciliation.ReconciliationResult<Orchestrator.Infrastructure.Kubernetes.V1AgentRun>;

namespace Orchestrator.Infrastructure.Kubernetes;

// Its permissions are the Role in deploy/orchestrator.yaml.
public sealed class AgentRunController(IKubernetesClient client, AgentSettings settings, ILogger<AgentRunController> logger)
    : IEntityController<V1AgentRun>
{
    // ponytail: polls queued runs and running Jobs; watch Jobs instead if 30s latency ever matters.
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);

    public async Task<Result> ReconcileAsync(V1AgentRun run, CancellationToken cancellationToken) =>
        run.Status.Phase switch
        {
            Phases.Succeeded or Phases.Failed => Result.Success(run),
            Phases.Running => await FollowJobAsync(run, cancellationToken),
            _ => await StartWhenFirstInLineAsync(run, cancellationToken),
        };

    // The Job and its pod are garbage collected through their owner reference, so deleting a run cancels it.
    public Task<Result> DeletedAsync(V1AgentRun run, CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success(run));

    /// <summary>
    /// One run per issue at a time, oldest first: concurrent runs on an issue would race on its feature branch.
    /// </summary>
    public static bool IsFirstInLine(V1AgentRun run, IEnumerable<V1AgentRun> runs) =>
        !runs.Any(other => other.Uid() != run.Uid()
            && other.Spec.Repository == run.Spec.Repository
            && other.Spec.Issue == run.Spec.Issue
            && other.Status.Phase is not (Phases.Succeeded or Phases.Failed)
            && (other.CreationTimestamp(), other.Name()).CompareTo((run.CreationTimestamp(), run.Name())) < 0);

    private async Task<Result> StartWhenFirstInLineAsync(V1AgentRun run, CancellationToken cancellationToken)
    {
        // ponytail: lists every run in the namespace; index them by an issue label once there are thousands.
        var runs = await client.ListAsync<V1AgentRun>(run.Namespace(), cancellationToken: cancellationToken);
        if (!IsFirstInLine(run, runs))
        {
            if (run.Status.Phase != Phases.Queued)
            {
                run.Status.Phase = Phases.Queued;
                run = await client.UpdateStatusAsync(run, cancellationToken);
            }

            return Result.Success(run, PollInterval);
        }

        try
        {
            await client.CreateAsync(BuildJob(run), cancellationToken);
        }
        catch (HttpOperationException e) when (e.Response.StatusCode == HttpStatusCode.Conflict)
        {
            // Created before the operator restarted, but the status update didn't make it.
        }

        run.Status.Phase = Phases.Running;
        run.Status.StartedAt = DateTime.UtcNow;
        run = await client.UpdateStatusAsync(run, cancellationToken);
        logger.LogInformation("Started {Run} with {Command} for {Repository}#{Issue}",
            run.Name(), run.Spec.Command, run.Spec.Repository, run.Spec.Issue);
        return Result.Success(run, PollInterval);
    }

    private async Task<Result> FollowJobAsync(V1AgentRun run, CancellationToken cancellationToken)
    {
        var job = await client.GetAsync<V1Job>(run.Name(), run.Namespace(), cancellationToken);
        var finished = job?.Status?.Conditions?.FirstOrDefault(c => c.Type is "Complete" or "Failed" && c.Status == "True");
        if (job is not null && finished is null)
            return Result.Success(run, PollInterval);

        var result = await ReadResultAsync(run, cancellationToken);
        run.Status.Phase = finished?.Type == "Complete" ? Phases.Succeeded : Phases.Failed;
        run.Status.FinishedAt = DateTime.UtcNow;
        run.Status.CostUsd = result?.CostUsd;
        run.Status.Turns = result?.Turns;
        run.Status.Message = finished?.Message ?? "The Job was deleted before it finished.";
        run = await client.UpdateStatusAsync(run, cancellationToken);
        logger.LogInformation("{Run} {Phase}: {Message}", run.Name(), run.Status.Phase, run.Status.Message);
        return Result.Success(run);
    }

    // The entrypoint writes Claude's result summary as the container's termination message.
    private async Task<AgentResult?> ReadResultAsync(V1AgentRun run, CancellationToken cancellationToken)
    {
        var pods = await client.ListAsync<V1Pod>(
            run.Namespace(), $"batch.kubernetes.io/job-name={run.Name()}", cancellationToken: cancellationToken);
        var message = pods
            .SelectMany(pod => pod.Status?.ContainerStatuses ?? [])
            .Select(container => container.State?.Terminated?.Message)
            .FirstOrDefault(m => !string.IsNullOrWhiteSpace(m));
        try
        {
            return message is null ? null : JsonSerializer.Deserialize<AgentResult>(message, JsonSerializerOptions.Web);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private V1Job BuildJob(V1AgentRun run) => new V1Job
    {
        Metadata = new V1ObjectMeta { Name = run.Name(), NamespaceProperty = run.Namespace() },
        Spec = new V1JobSpec
        {
            // Never retried: a run pushes branches and comments on the issue.
            BackoffLimit = 0,
            ActiveDeadlineSeconds = (long)TimeSpan.FromHours(4).TotalSeconds,
            // Keeps the finished pod, and so its logs, for a day.
            TtlSecondsAfterFinished = (int)TimeSpan.FromDays(1).TotalSeconds,
            Template = new V1PodTemplateSpec
            {
                Spec = new V1PodSpec
                {
                    RestartPolicy = "Never",
                    // The agent runs whatever it likes, so it gets no credentials for the cluster API.
                    AutomountServiceAccountToken = false,
                    // The image's app user, which owns the Claude plugins in its home directory.
                    SecurityContext = new V1PodSecurityContext { RunAsNonRoot = true, RunAsUser = 1654 },
                    Containers =
                    [
                        new V1Container
                        {
                            Name = "agent",
                            Image = settings.Image,
                            Args = [run.Spec.Prompt],
                            Env =
                            [
                                new V1EnvVar { Name = "REPO", Value = run.Spec.Repository },
                                new V1EnvVar { Name = "ISSUE", Value = run.Spec.Issue.ToString() },
                            ],
                            EnvFrom = [new V1EnvFromSource { SecretRef = new V1SecretEnvSource { Name = settings.SecretName } }],
                            Resources = new V1ResourceRequirements
                            {
                                Requests = new Dictionary<string, ResourceQuantity> { ["cpu"] = new("1"), ["memory"] = new("2Gi") },
                            },
                            // No volume over /workspace: git refuses a repo in a directory the app user doesn't own,
                            // and a mounted volume is owned by root.
                        },
                    ],
                },
            },
        },
    }.WithOwnerReference(run);

    private sealed record AgentResult(double? CostUsd, int? Turns);
}
