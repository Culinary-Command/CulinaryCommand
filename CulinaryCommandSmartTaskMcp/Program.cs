using CulinaryCommandSmartTaskMcp.Services;
using CulinaryCommandSmartTaskMcp.Tools;
using Google.GenAI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

// ---------------------------------------------------------------------------
// SmartTask MCP Server — stdio transport
//
// Boots when the project is run as a console app (e.g. `dotnet run`). When the
// same assembly is invoked by the AWS Lambda runtime, this Main is bypassed —
// Lambda calls Function.Handle directly via the handler configured in
// aws-lambda-tools-defaults.json.
// ---------------------------------------------------------------------------

var builder = Host.CreateApplicationBuilder(args);

// MCP frames flow over stdout — push every log to stderr so logs don't get
// parsed as protocol frames by the connected client (Claude Desktop, the MCP
// Inspector, etc.).
builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});

// Optional Gemini classifier. If a key is present, generate_plan and
// classify_service_window will use it as a tiebreaker for ambiguous recipe
// titles; otherwise the server runs heuristic-only.
var geminiApiKey =
    Environment.GetEnvironmentVariable("GOOGLE_API_KEY")
    ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY");

if (!string.IsNullOrWhiteSpace(geminiApiKey))
{
    builder.Services.AddSingleton(new Client(apiKey: geminiApiKey));
    builder.Services.AddSingleton(sp =>
        new GeminiPlanner(sp.GetRequiredService<Client>()));
}
// If no key is configured we simply do not register a GeminiPlanner. Consumers
// resolve it via GetService<GeminiPlanner>() which returns null.

builder.Services.AddSingleton<HeuristicFallback>();
builder.Services.AddSingleton<ServiceWindowClock>();
builder.Services.AddSingleton(sp =>
    new SmartTaskPlanner(
        sp.GetRequiredService<HeuristicFallback>(),
        sp.GetRequiredService<ServiceWindowClock>(),
        sp.GetService<GeminiPlanner>()));

// Register the MCP server with stdio transport. The [McpServerTool] methods on
// SmartTaskTools are auto-discovered and exposed over the MCP protocol.
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<SmartTaskTools>();

await builder.Build().RunAsync();
