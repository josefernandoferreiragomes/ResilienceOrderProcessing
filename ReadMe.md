# Resilient Order Processing API with SignalR Logging

A minimal API implementation showcasing resilience patterns with Polly, real-time logging via SignalR, and feature toggles.

## 🏗️ Architecture

```
┌─────────────────┐    HTTP/SignalR    ┌──────────────────┐
│ OrderProcessing │ ────────────────── │   LoggingApi     │
│      API        │                    │                  │
│  (Minimal API)  │                    │ (SignalR Hubs)   │
└─────────────────┘                    └──────────────────┘
         │                                       │
         │                              ┌──────────────────┐
         └──────────────────────────────│ Real-time        │
                                        │ Dashboard        │
                                        │ (SignalR Client) │
                                        └──────────────────┘
```

## 🚀 Features

### OrderProcessing.Api (Minimal API)
- ✅ **Minimal API endpoints** with comprehensive OpenAPI documentation
- ✅ **Polly resilience patterns** (Retry + Circuit Breaker) for external calls
- ✅ **Real-time logging** via SignalR integration
- ✅ **Performance monitoring** with automatic operation timing
- ✅ **Request correlation** with unique request IDs
- ✅ **Feature toggle integration** for dynamic configuration
- ✅ **Comprehensive error handling** with proper HTTP status codes
- ✅ **Swagger UI** at root path for easy testing

### LoggingApi
- ✅ **SignalR Hubs** for real-time log broadcasting
- ✅ **Feature toggle management** with REST endpoints
- ✅ **Multiple log categories** (General, Error, Performance, Detailed)
- ✅ **Dynamic configuration** without API restart

### Real-time Dashboard
- ✅ **Live log streaming** with color-coded severity levels
- ✅ **Feature toggle controls** with instant updates
- ✅ **Performance statistics** and request metrics
- ✅ **Modern glassmorphism UI** with responsive design

## 📋 API Endpoints

### OrderProcessing.Api

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/` | Swagger UI (root path) |
| `GET` | `/health` | Health check endpoint |
| `GET` | `/api/features` | Get feature toggle status |
| `POST` | `/api/orders` | Create a new order |
| `GET` | `/api/orders/{orderId}` | Get order by ID |
| `POST` | `/api/orders/{orderId}/cancel` | Cancel an order |

### LoggingApi

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/api/logging/log` | Send log message |
| `POST` | `/api/logging/performance` | Send performance log |
| `GET` | `/api/featuretoggle` | Get all feature toggles |
| `GET` | `/api/featuretoggle/{name}` | Get specific feature status |
| `POST` | `/api/featuretoggle/{name}/toggle` | Toggle feature state |
| `GET` | `/loggingHub` | SignalR hub endpoint |

## 🛠️ Setup Instructions

### Prerequisites
- .NET 8.0 SDK
- Visual Studio 2022 or VS Code

### 1. Clone and Setup
```bash
git clone https://github.com/josefernandoferreiragomes/ResilienceOrderProcessing.git
cd ResilienceOrderProcessing
```

### 2. Add the New Files
Create the following directory structure:
```
OrderProcessing.Api/
├── Services/
│   ├── ISignalRLoggingService.cs
│   └── SignalRLoggingService.cs
├── Middleware/
│   └── LoggingMiddleware.cs
├── Models/
│   └── CreateOrderRequest.cs
├── Program.cs (replace existing)
├── appsettings.json (update)
└── test-endpoints.http (new)
```

### 3. Update Project Files
Replace your existing `OrderProcessing.Api.csproj` and add the package references shown in the artifacts.

### 4. Start the Applications

#### Terminal 1 - LoggingApi
```bash
cd LoggingApi
dotnet run
```
LoggingApi will start on: `https://localhost:7002`

#### Terminal 2 - OrderProcessing.Api
```bash
cd OrderProcessing.Api
dotnet run
```
OrderProcessing.Api will start on: `https://localhost:7103`

### 5. Test the Solution

#### Option 1: Swagger UI
- Navigate to `https://localhost:7103` (OrderProcessing.Api)
- Use the interactive Swagger UI to test endpoints

#### Option 2: HTTP File (VS Code)
- Install "REST Client" extension
- Open `test-endpoints.http`
- Click "Send Request" above each endpoint

#### Option 3: Real-time Dashboard
- Open `dashboard.html` in a browser
- Click "Connect" to establish SignalR connection
- Test API endpoints and watch logs stream in real-time

## 🧪 Testing Examples

### Create Order
```http
POST https://localhost:7103/api/orders
Content-Type: application/json

{
    "customerId": "customer-123",
    "productId": "product-456",
    "quantity": 2,
    "price": 29.99,
    "notes": "Priority shipping"
}
```

### Get Order
```http
GET https://localhost:7103/api/orders/some-order-id
```

### Cancel Order (20% chance of failure for demo)
```http
POST https://localhost:7103/api/orders/some-order-id/cancel
```

### Feature Toggle
```http
POST https://localhost:7002/api/featuretoggle/RealTimeLogging/toggle
Content-Type: application/json

true
```

## 🔧 Key Implementation Details

### Minimal API Structure
- **Clean endpoints** using `app.MapGroup()` for organization
- **Built-in validation** using data annotations
- **Comprehensive error handling** with `Results.*` patterns
- **OpenAPI documentation** with detailed summaries and descriptions

### Resilience Patterns
- **Retry Policy**: 3 attempts with exponential backoff
- **Circuit Breaker**: Opens after 3 consecutive failures, 30-second break
- **Timeout Handling**: 30-second HTTP client timeout
- **Graceful Degradation**: API continues working if logging service is down

### Performance Monitoring
- **Request timing** for all operations
- **Performance logs** sent to SignalR
- **Statistics tracking** in the dashboard
- **Correlation IDs** for distributed tracing

### Feature Toggles
- **Real-time configuration** without restarts
- **Graceful fallbacks** when toggle service is unavailable
- **Multiple toggle types**: RealTimeLogging, DetailedErrorLogging, PerformanceLogging

## 📊 Monitoring and Observability

### Log Categories
- **Request**: HTTP request/response logs
- **Order**: Business logic logs
- **Validation**: Input validation logs
- **Performance**: Operation timing logs
- **Health**: System health logs

### Real-time Dashboard Features
- **Live log streaming** with different severity colors
- **Feature toggle controls** for dynamic configuration
- **Statistics panels** showing request counts, error rates, response times
- **Log filtering** by category and severity
- **Auto-scrolling** with log history management

## 🐳 Docker Support

### Build and Run
```bash
docker-compose up --build
```

### Services
- **OrderProcessing.Api**: `http://localhost:7103`
- **LoggingApi**: `http://localhost:7002`
- **Nginx Proxy**: `http://localhost` (optional)

## 🚨 Error Scenarios

The API includes intentional error scenarios for testing resilience:

1. **Invalid Input**: Empty CustomerIds, zero quantities
2. **Random Failures**: 20% chance of failure in cancel operations
3. **Network Issues**: Circuit breaker triggers after consecutive failures
4. **Service Unavailable**: Graceful handling when LoggingApi is down

## 🎯 Next Steps

1. **Add Authentication**: JWT tokens with role-based access
2. **Database Integration**: Entity Framework with SQL Server
3. **Message Queues**: Azure Service Bus for reliable messaging
4. **Distributed Tracing**: OpenTelemetry integration
5. **Health Checks**: Advanced health monitoring with dependencies
6. **Rate Limiting**: API throttling with Redis
7. **Caching**: Distributed caching for performance
8. **Testing**: Unit tests with xUnit and integration tests

## 📚 References

- [ASP.NET Core Minimal APIs](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis)
- [Polly Resilience Patterns](https://www.pollydocs.org/)
- [SignalR Real-time Communication](https://learn.microsoft.com/en-us/aspnet/core/signalr/introduction)
- [Feature Management in .NET](https://learn.microsoft.com/en-us/azure/azure-app-configuration/feature-management-dotnet-reference)