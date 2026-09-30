var app = WebApplication.CreateBuilder(args).Build();
app.MapGet("/", () => "hello");
app.Run();
