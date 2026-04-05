using Miniclip.Core.ServiceDefaults;
using Miniclip.Simulator.ReadModels.WebJob;

var builder = Host.CreateApplicationBuilder(args);

builder.AddStructuredLogging();

var startup = new Startup(builder.Configuration);
startup.ConfigureServices(builder.Services);

var host = builder.Build();
startup.Configure(host);

host.Run();
