using Miniclip.Core.ServiceDefaults;
using Miniclip.Core.ServiceDefaults.Configuration;
using Miniclip.Simulator.Api;
using Miniclip.Simulator.Api.Infrastructure.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.AddStructuredLogging();

var startup = new Startup(builder.Configuration);
startup.ConfigureServices(builder.Services);

var app = builder.Build();

startup.Configure(app);
app.MapDefaultEndpoints();

app.Run();
