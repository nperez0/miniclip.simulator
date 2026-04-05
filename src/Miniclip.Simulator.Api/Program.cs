using Miniclip.Core.ServiceDefaults;
using Miniclip.Simulator.Api;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var startup = new Startup(builder.Configuration);
startup.ConfigureServices(builder.Services);

var app = builder.Build();

startup.Configure(app);
app.MapDefaultEndpoints();

app.Run();
