using Miniclip.Simulator.AppHost.Extensions;

var builder = DistributedApplication.CreateBuilder(args);

var mysqlPassword = builder.AddParameter("mysql-password", secret: true);

var mysql = builder.AddMySql("mysql", password: mysqlPassword, port: 3306)
    .WithDataVolume();

var readDb = mysql.AddDatabase("SimulatorRead", "MiniclipSimulator_Read");

builder.AddContainer("kurrentdb", "kurrentplatform/kurrentdb")
    .WithImageTag("latest")
    .WithEnvironment("KURRENTDB_CLUSTER_SIZE", "1")
    .WithEnvironment("KURRENTDB_RUN_PROJECTIONS", "All")
    .WithEnvironment("KURRENTDB_START_STANDARD_PROJECTIONS", "true")
    .WithEnvironment("KURRENTDB_INSECURE", "true")
    .WithEnvironment("KURRENTDB_ENABLE_ATOM_PUB_OVER_HTTP", "true")
    .WithHttpEndpoint(port: 2113, targetPort: 2113, name: "http")
    .WithVolume("kurrentdb-data", "/var/lib/kurrentdb");

var kafka = builder.AddKafka("kafka")
    .WithKafkaUI();

var kafkaTopics = kafka.WithTopicCreation();

var webjob = builder.AddProject<Projects.Miniclip_Simulator_ReadModels_WebJob>("simulator-readmodels-webjob")
    .WithReference(readDb)
    .WithReference(kafka)
    .WaitFor(readDb)
    .WaitFor(kafkaTopics)
    .WithHttpEndpoint(port: 8081, targetPort: 18081, name: "health")
    .WithEnvironment("HEALTHCHECK_HTTP_PORT_LISTENER", "18081");

builder.AddProject<Projects.Miniclip_Simulator_Api>("simulator-api")
    .WithReference(readDb)
    .WithReference(kafka)
    .WaitFor(readDb)
    .WaitFor(kafkaTopics)
    .WaitFor(webjob);


builder.Build().Run();
