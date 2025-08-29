var builder = DistributedApplication.CreateBuilder(args);

var mockInventoryService = builder.AddProject<Projects.MockInventoryService_Api>("mockinventoryservice-api");

var orderProcessing = builder.AddProject<Projects.OrderProcessing_Api>("orderprocessing-api");

orderProcessing.WithReference(mockInventoryService);

builder.AddProject<Projects.LoggingApi>("loggingapi");

builder.Build().Run();
