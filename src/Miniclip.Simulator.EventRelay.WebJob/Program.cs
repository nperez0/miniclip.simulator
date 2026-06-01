using Miniclip.Core.ServiceDefaults.Configuration;
using Miniclip.Simulator.EventRelay.WebJob;

var builder = Host.CreateApplicationBuilder(args);

builder.AddStructuredLogging();

var startup = new Startup(builder.Configuration);
startup.ConfigureServices(builder.Services);

builder.Build().Run();