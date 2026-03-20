var builder = DistributedApplication.CreateBuilder(args);

var mysqlPassword = builder.AddParameter("mysql-password", secret: true);

var mysql = builder.AddMySql("mysql", password: mysqlPassword, port: 3306)
    .WithDataVolume();

var writeDb = mysql.AddDatabase("SimulatorWrite", "MiniclipSimulator_Write");
var readDb = mysql.AddDatabase("SimulatorRead", "MiniclipSimulator_Read");

builder.AddProject<Projects.Miniclip_Simulator_Api>("simulator-api")
    .WithReference(writeDb)
    .WithReference(readDb)
    .WaitFor(writeDb)
    .WaitFor(readDb);

builder.Build().Run();
