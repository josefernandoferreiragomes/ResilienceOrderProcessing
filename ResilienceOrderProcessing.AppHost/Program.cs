var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.MockInventoryService_Api>("mockinventoryservice-api");

builder.AddProject<Projects.OrderProcessing_Api>("orderprocessing-api");

builder.Build().Run();
