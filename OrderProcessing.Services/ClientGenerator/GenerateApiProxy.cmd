@echo off
:: This command uses NSwag CLI to generate a C# client from the ApiServer Swagger spec.

nswag openapi2csclient ^
/input:https://localhost:7261/swagger/v1/swagger.json ^
/output:ApiProxies\InventoryServiceClient.cs ^
/namespace:OrderProcessing.Api.Clients

echo Web API client proxy generated.
pause
