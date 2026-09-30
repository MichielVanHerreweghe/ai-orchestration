using System.Text.Json;
using Orchestrator.Application.Webhooks;
using KubeOps.Operator;
using Orchestrator.Domain;
using Orchestrator.Infrastructure.GitHub;
using Orchestrator.Infrastructure.Kubernetes;

var builder = WebApplication.CreateBuilder(args);

string Required(string key) =>
    builder.Configuration[key] ?? throw new InvalidOperationException($"{key} is not configured.");

builder.Services.AddSingleton(new GitHubSignatureVerifier(Required("GitHub:WebhookSecret")));
builder.Services.AddSingleton(new AgentSettings(Required("Agent:Image"), Required("Agent:SecretName")));
builder.Services.AddSingleton<IAgentRunner, KubernetesAgentRunner>();
builder.Services.AddSingleton<WebhookEventHandler>();
// In the cluster, the operator only watches its own namespace; run locally, it watches the whole cluster.
builder.Services
    .AddKubernetesOperator(settings => settings.Namespace = builder.Configuration["POD_NAMESPACE"])
    .AddController<AgentRunController, V1AgentRun>();

var app = builder.Build();

app.MapPost("/webhooks/github", async (
    HttpRequest request,
    GitHubSignatureVerifier verifier,
    WebhookEventHandler handler,
    CancellationToken cancellationToken) =>
{
    using var buffer = new MemoryStream();
    await request.Body.CopyToAsync(buffer, cancellationToken);
    var body = buffer.ToArray();

    if (!verifier.IsValid(body, request.Headers["X-Hub-Signature-256"]))
        return Results.Unauthorized();

    var eventName = request.Headers["X-GitHub-Event"].ToString();
    var deliveryId = request.Headers["X-GitHub-Delivery"].ToString();
    if (eventName.Length == 0 || deliveryId.Length == 0)
        return Results.BadRequest("Missing X-GitHub-Event or X-GitHub-Delivery header.");

    JsonElement payload;
    try
    {
        using var document = JsonDocument.Parse(body);
        payload = document.RootElement.Clone();
    }
    catch (JsonException)
    {
        return Results.BadRequest("Payload must be JSON; set the webhook content type to application/json.");
    }

    await handler.HandleAsync(new WebhookEvent(deliveryId, eventName, payload), cancellationToken);
    return Results.Ok();
});

app.Run();
