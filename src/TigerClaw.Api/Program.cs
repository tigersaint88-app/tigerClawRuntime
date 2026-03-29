using Microsoft.Extensions.DependencyInjection;
using TigerClaw.Core;
using TigerClaw.Infrastructure;
using TigerClaw.Infrastructure.Options;
using TigerClaw.Memory;
using TigerClaw.Models;
using TigerClaw.Runtime;
using TigerClaw.Skills;
using TigerClaw.Workflows;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<TigerClawOptions>(builder.Configuration.GetSection("TigerClaw"));
builder.Services.PostConfigure<TigerClawOptions>(opts =>
{
    var root = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", ".."));
    opts.Database.DataDirectory = Path.Combine(root, "data");
    opts.Database.ConnectionString = $"Data Source={Path.Combine(opts.Database.DataDirectory, "tigerclaw.db")}";
    opts.Workspace.RootPath = root;
    opts.Workspace.SkillsPath = "skills";
    opts.Workspace.WorkflowsPath = "workflows";
    opts.Workspace.ArtifactsPath = "artifacts";
});

builder.Services.AddTigerClawInfrastructure();
builder.Services.AddTigerClawMemory();
builder.Services.AddTigerClawSkills(isInteractive: false);
builder.Services.AddTigerClawWorkflows();
builder.Services.AddTigerClawRuntime();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.MapPost("/tasks/run", async (RunTaskRequest req, IRuntimeFacade facade) =>
{
    var request = new TaskRequest
    {
        RequestId = Guid.NewGuid().ToString("N"),
        SessionId = req.SessionId ?? Guid.NewGuid().ToString("N"),
        UserId = req.UserId ?? "api-user",
        InputText = req.InputText ?? "",
        Channel = "api"
    };
    var response = await facade.RunTaskAsync(request);
    return Results.Json(response);
});

app.MapPost("/workflows/{id}/run", async (string id, RunWorkflowRequest? req, IRuntimeFacade facade) =>
{
    var inputs = req?.Inputs ?? new Dictionary<string, object?>();
    var response = await facade.RunWorkflowAsync(id, inputs, req?.UserId);
    return Results.Json(response);
});

app.MapGet("/skills", async (IRuntimeFacade facade) =>
{
    var skills = await facade.ListSkillsAsync();
    return Results.Json(skills);
});

app.MapGet("/workflows", async (IRuntimeFacade facade) =>
{
    var workflows = await facade.ListWorkflowsAsync();
    return Results.Json(workflows);
});

app.MapGet("/memory/preferences", async (string? userId, IMemoryStore memory) =>
{
    var prefs = await memory.Preferences.ListAllAsync(userId);
    return Results.Json(prefs);
});

app.MapPost("/memory/preferences", async (SavePreferenceRequest req, IMemoryStore memory) =>
{
    await memory.Preferences.UpsertAsync(req.Key, req.Value ?? "", req.UserId);
    return Results.Ok();
});

await EnsureDatabaseAsync(app.Services);
app.Run();

static async Task EnsureDatabaseAsync(IServiceProvider sp)
{
    try
    {
        var init = sp.GetRequiredService<TigerClaw.Infrastructure.Database.DatabaseInitializer>();
        await init.InitializeAsync();
    }
    catch (Exception) { /* ignore */ }
}

public record RunTaskRequest(string? SessionId, string? UserId, string? InputText);
public record RunWorkflowRequest(string? UserId, Dictionary<string, object?>? Inputs);
public record SavePreferenceRequest(string Key, string? Value, string? UserId);
