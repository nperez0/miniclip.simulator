using Miniclip.Core.ServiceDefaults;
using Miniclip.Simulator.Api;

var builder = WebApplication.CreateBuilder(args);

builder.AddStructuredLogging();

var startup = new Startup(builder.Configuration);
startup.ConfigureServices(builder.Services);
builder.AddServiceDefaults();

var app = builder.Build();

startup.Configure(app);
app.MapDefaultEndpoints();

app.Run();
