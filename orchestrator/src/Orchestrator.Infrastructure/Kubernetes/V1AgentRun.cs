using k8s.Models;
using KubeOps.Abstractions.Entities;
using KubeOps.Abstractions.Entities.Attributes;

namespace Orchestrator.Infrastructure.Kubernetes;

/// <summary>One agent run for one command comment; the operator runs it as a Job of the same name.</summary>
[KubernetesEntity(Group = "orchestrator.axxit.be", ApiVersion = "v1alpha1", Kind = "AgentRun", PluralName = "agentruns")]
[GenericAdditionalPrinterColumn(".spec.repository", "Repository", "string")]
[GenericAdditionalPrinterColumn(".spec.issue", "Issue", "integer")]
[GenericAdditionalPrinterColumn(".spec.command", "Command", "string")]
[GenericAdditionalPrinterColumn(".status.phase", "Phase", "string")]
[GenericAdditionalPrinterColumn(".status.costUsd", "Cost", "number")]
[GenericAdditionalPrinterColumn(".status.turns", "Turns", "integer")]
[GenericAdditionalPrinterColumn(".metadata.creationTimestamp", "Age", "date")]
public partial class V1AgentRun : CustomKubernetesEntity<V1AgentRun.EntitySpec, V1AgentRun.EntityStatus>
{
    public static class Phases
    {
        public const string Queued = "Queued";
        public const string Running = "Running";
        public const string Succeeded = "Succeeded";
        public const string Failed = "Failed";
    }

    public sealed class EntitySpec
    {
        public string Repository { get; set; } = "";

        public int Issue { get; set; }

        public string Command { get; set; } = "";

        public string Prompt { get; set; } = "";
    }

    public sealed class EntityStatus
    {
        public string? Phase { get; set; }

        public double? CostUsd { get; set; }

        public int? Turns { get; set; }

        public DateTime? StartedAt { get; set; }

        public DateTime? FinishedAt { get; set; }

        public string? Message { get; set; }
    }
}
