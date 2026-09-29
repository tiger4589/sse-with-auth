var builder = DistributedApplication.CreateBuilder(args);

var keycloak = builder.AddKeycloak("keycloak", 8080)
    .WithLifetime(ContainerLifetime.Persistent);

var api = builder.AddProject<Projects.AwesomeApi>("awesomeapi")
    .WithReference(keycloak)
    .WaitFor(keycloak);

builder.AddProject<Projects.AwesomeWeb>("notificationweb")
    .WithReference(keycloak)
    .WaitFor(keycloak)
    .WithReference(api)
    .WaitFor(api);

builder.Build().Run();
