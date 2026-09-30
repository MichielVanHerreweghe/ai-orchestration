using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<NotesDb>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("Db")));
var app = builder.Build();

// SQL Server takes a while to accept connections after its container starts.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<NotesDb>();
    for (var attempt = 1; ; attempt++)
    {
        try
        {
            await db.Database.EnsureCreatedAsync();
            break;
        }
        catch (Exception e) when (attempt < 60)
        {
            app.Logger.LogInformation("Database not ready ({Message}); retrying", e.Message);
            await Task.Delay(TimeSpan.FromSeconds(2));
        }
    }
}

app.MapGet("/api/notes", (NotesDb db) => db.Notes.OrderBy(note => note.IsDone).ThenByDescending(note => note.Id).ToListAsync());
app.MapPost("/api/notes", async (Note note, NotesDb db) =>
{
    db.Notes.Add(note);
    await db.SaveChangesAsync();
    return Results.Created($"/api/notes/{note.Id}", note);
});
app.MapPatch("/api/notes/{id}", async (int id, NoteDone body, NotesDb db) =>
{
    var note = await db.Notes.FindAsync(id);
    if (note is null) return Results.NotFound();
    note.IsDone = body.IsDone;
    await db.SaveChangesAsync();
    return Results.Ok(note);
});

app.Run();

public sealed class NotesDb(DbContextOptions<NotesDb> options) : DbContext(options)
{
    public DbSet<Note> Notes => Set<Note>();
}

public sealed class Note
{
    public int Id { get; set; }

    public string Text { get; set; } = "";

    public bool IsDone { get; set; }
}

public sealed record NoteDone(bool IsDone);
