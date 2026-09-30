using k8s.Models;
using Orchestrator.Infrastructure.Kubernetes;

namespace Orchestrator.Tests;

public class AgentRunControllerTests
{
    private static V1AgentRun Run(string name, int minute, int issue = 7, string? phase = null) => new()
    {
        Metadata = new V1ObjectMeta { Name = name, Uid = name, CreationTimestamp = new DateTime(2026, 1, 1, 0, minute, 0) },
        Spec = new V1AgentRun.EntitySpec { Repository = "owner/repo", Issue = issue },
        Status = new V1AgentRun.EntityStatus { Phase = phase },
    };

    [Fact]
    public void IsFirstInLine_WaitsForAnOlderUnfinishedRunOnTheSameIssue()
    {
        // Arrange
        var older = Run("a", minute: 1, phase: V1AgentRun.Phases.Running);
        var newer = Run("b", minute: 2);

        // Act
        var olderFirst = AgentRunController.IsFirstInLine(older, [older, newer]);
        var newerFirst = AgentRunController.IsFirstInLine(newer, [older, newer]);

        // Assert
        Assert.True(olderFirst);
        Assert.False(newerFirst);
    }

    [Fact]
    public void IsFirstInLine_IgnoresFinishedRunsAndOtherIssues()
    {
        // Arrange
        var run = Run("c", minute: 3);
        var others = new[]
        {
            Run("a", minute: 1, phase: V1AgentRun.Phases.Failed),
            Run("b", minute: 2, issue: 8, phase: V1AgentRun.Phases.Running),
            run,
        };

        // Act
        var first = AgentRunController.IsFirstInLine(run, others);

        // Assert
        Assert.True(first);
    }

    [Fact]
    public void IsFirstInLine_BreaksCreationTimeTiesByName()
    {
        // Arrange
        var a = Run("a", minute: 1);
        var b = Run("b", minute: 1);

        // Act
        var aFirst = AgentRunController.IsFirstInLine(a, [a, b]);
        var bFirst = AgentRunController.IsFirstInLine(b, [a, b]);

        // Assert
        Assert.True(aFirst);
        Assert.False(bFirst);
    }
}
