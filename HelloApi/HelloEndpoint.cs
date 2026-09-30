using FastEndpoints;

sealed class HelloEndpoint : EndpointWithoutRequest
{
    public override void Configure()
    {
        Get("/");
        AllowAnonymous();
    }

    public override Task HandleAsync(CancellationToken ct) => Send.StringAsync("hello", cancellation: ct);
}
