var builder = DistributedApplication.CreateBuilder(args);

var mysqlPassword = builder.AddParameter("mysql-password", secret: true);

var mysql = builder.AddMySql("mysql", password: mysqlPassword, port: 3306)
    .WithDataVolume();

var writeDb = mysql.AddDatabase("SimulatorWrite", "MiniclipSimulator_Write");
var readDb = mysql.AddDatabase("SimulatorRead", "MiniclipSimulator_Read");

builder.AddContainer("eventstoredb", "eventstore/eventstore")
    .WithImageTag("24.10-bookworm-slim")
    .WithEnvironment("EVENTSTORE_CLUSTER_SIZE", "1")
    .WithEnvironment("EVENTSTORE_RUN_PROJECTIONS", "All")
    .WithEnvironment("EVENTSTORE_START_STANDARD_PROJECTIONS", "true")
    .WithEnvironment("EVENTSTORE_INSECURE", "true")
    .WithEnvironment("EVENTSTORE_ENABLE_ATOM_PUB_OVER_HTTP", "true")
    .WithHttpEndpoint(port: 2113, targetPort: 2113, name: "http")
    .WithVolume("eventstore-data", "/var/lib/eventstore");

builder.AddProject<Projects.Miniclip_Simulator_Api>("simulator-api")
    .WithReference(writeDb)
    .WithReference(readDb)
    .WaitFor(writeDb)
    .WaitFor(readDb);

builder.Build().Run();
