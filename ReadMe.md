# Resilient Order Processing API with SignalR Logging

A comprehensive order processing API implementation showcasing modern resilience patterns with Polly, real-time logging via SignalR, distributed tracing with OpenTelemetry, and feature toggles using .NET Aspire service defaults.

## 🏗️ Architecture

```
┌─────────────────────────────┐      HTTP/SignalR       ┌──────────────────────────────┐
│  OrderProcessing.Api        │◄──────────────────────► │  LoggingApi / LoggingHub     │
│  (Minimal APIs)             │                         │  (SignalR + Feature Toggles) │
│  ┌───────────────────────┐  │                         │  ┌──────────────────────┐   │
│  │ Resilience Patterns   │  │                         │  │ Real-time Logging    │   │
│  │ ├─ Polly (Retry)      │  │                         │  │ ├─ Log Broadcasting  │   │
│  │ ├─ Circuit Breaker    │  │                         │  │ ├─ Feature Toggles   │   │
│  │ └─ Bulkhead           │  │                         │  │ └─ Performance Logs  │   │
│  └───────────────────────┘  │                         │  └──────────────────────┘   │
│  ┌───────────────────────┐  │                         │  ┌──────────────────────┐   │
│  │ Observability         │  │                         │  │ Service Coordination │   │
│  │ ├─ OpenTelemetry      │  │                         │  │ ├─ Health Checks     │   │
│  │ ├─ Request Tracing    │  │                         │  │ ├─ Service Discovery │   │
│  │ └─ Metrics            │  │                         │  │ └─ Resilience        │   │
│  └───────────────────────┘  │                         │  └──────────────────────┘   │
└─────────────────────────────┘                         └──────────────────────────────┘
          │                                                        │
          │         ┌──────────────────────────────────────┐     │
          └────────►│  OpenTelemetry Exporter (OTLP)       │◄────┘
                    │  └─ Metrics & Traces Export          │
                    └──────────────────────────────────────┘
                                    │
                    ┌───────────────┴────────────────┐
                    │                                │
          ┌─────────▼──────────┐        ┌───────────▼───────┐
          │  Real-time Logging │        │  Dashboard UI     │
          │  (SignalR Client)  │        │  (Aspire-ready)   │
          └────────────────────┘        └───────────────────┘
```

## 🚀 Features

### OrderProcessing.Api (Minimal API) - .NET 10

#### ✅ Core API Functionality
- **Minimal API endpoints** with comprehensive OpenAPI documentation
- **Full CRUD operations** for orders (Create, Retrieve, Update, Cancel, Process)
- **Paginated order retrieval** with configurable page sizes
- **Customer-specific order queries** with filtering
- **Swagger/OpenAPI** integration at root path

#### ✅ Resilience Patterns (Polly)
- **Retry Policy**: Configurable exponential backoff with jitter (default: 3 attempts)
- **Circuit Breaker**: Automatic failure detection and recovery (opens after 3 failures, 30-second break)
- **Timeout Handling**: Per-service timeout configuration (Inventory: 5s, Payment: 10s, Shipping: 8s)
- **Bulkhead Isolation**: Concurrent request limiting per external service
- **Graceful Degradation**: Fallback mechanisms when services unavailable

#### ✅ Real-time Logging & Observability
- **SignalR Integration**: Real-time log streaming to connected clients
- **Request Correlation**: Unique RequestId per operation for tracing
- **Performance Monitoring**: Automatic operation timing (Stopwatch-based)
- **OpenTelemetry**: Full distributed tracing and metrics collection
  - ASP.NET Core instrumentation
  - HTTP client instrumentation
  - Runtime metrics (CPU, memory, GC)
- **Structured Logging**: Serilog integration with file persistence
- **Multiple Log Categories**: Request, Order, Validation, Performance, Health, Error

#### ✅ Feature Toggle Integration
- **Dynamic Configuration**: Real-time feature flags without API restart
- **Graceful Fallbacks**: Default values when toggle service unavailable
- **Toggle Types**: RealTimeLogging, DetailedErrorLogging, PerformanceLogging

#### ✅ Advanced Testing & Demo Features
- **Resilience Testing Endpoint** (`POST /api/demo/test-resilience`)
  - Sequential order creation with configurable delays
  - Detailed retry attempt tracking with metrics
- **Load Testing Endpoint** (`POST /api/demo/simulate-load`)
  - Concurrent order processing with proper service scoping
  - Throughput and response time metrics
- **Circuit Breaker Reset** (`POST /api/demo/reset-circuit-breakers`)
  - Demo-only endpoint for testing state transitions

#### ✅ Health Checks (ASP.NET Core)
- **Liveness Check** (`/alive`): Service responsiveness verification
- **Readiness Check** (`/health`): Full dependency validation
- **Database Integration**: Entity Framework DbContext checks
- **Resilience Monitoring**: Circuit breaker state health checks

#### ✅ Service Integration (via .NET Aspire)
- **Service Discovery**: Automatic service location resolution
- **Standard Resilience Handler**: Built-in Polly policies for all HTTP clients
- **Health Check Coordination**: Service-to-service dependency monitoring
- **OpenTelemetry Export**: OTLP endpoint support for centralized observability

### LoggingApi (.NET 10)

#### ✅ Real-time Capabilities
- **SignalR Hubs**: Broadcast logging to multiple connected clients
- **Feature Toggle Management**: REST endpoints for dynamic configuration
- **Performance Log Aggregation**: Collect metrics from OrderProcessing.Api
- **Log Streaming**: Support multiple severity levels (Information, Warning, Error)

#### ✅ External Service Integration
- **Inventory Service** (HTTP): Product availability checks and reservations
- **Payment Service**: Order payment processing with resilience
- **Shipping Service**: Shipment creation and tracking
- **Resilient Decorators**: All services wrapped with Polly policies

### Real-time Dashboard & UI
#### ✅ Implemented
- Basic test endpoint documentation in HTTP files
- API endpoint structure for dashboard integration

#### ⚠️ Missing (Future Enhancement)
- Interactive dashboard.html UI
- Live log visualization with filtering
- Feature toggle controls UI
- Performance statistics panels
- Real-time metrics display

---

## 📋 API Endpoints

### OrderProcessing.Api

#### Production Endpoints
| Method | Endpoint | Description | Status |
|--------|----------|-------------|--------|
| `GET` | `/` | Swagger UI documentation | ✅ |
| `GET` | `/health` | Full health check | ✅ |
| `GET` | `/alive` | Liveness check | ✅ |
| `GET` | `/api/features` | Get feature toggle status | ✅ |
| `POST` | `/api/orders` | Create new order | ✅ |
| `GET` | `/api/orders` | List all orders (paginated) | ✅ |
| `GET` | `/api/orders/{id}` | Get order by ID | ✅ |
| `GET` | `/api/orders/customer/{customerId}` | Get customer's orders | ✅ |
| `GET` | `/api/orders/page/{page}` | Get paginated orders | ✅ |
| `POST` | `/api/orders/{id}/process` | Complete order workflow | ✅ |
| `POST` | `/api/orders/{id}/cancel` | Cancel order | ✅ |
| `PUT` | `/api/orders/{id}/status` | Update order status | ✅ |

#### Testing/Demo Endpoints
| Method | Endpoint | Description | Status |
|--------|----------|-------------|--------|
| `POST` | `/api/demo/test-resilience` | Sequential resilience testing | ✅ |
| `POST` | `/api/demo/simulate-load` | Concurrent load testing | ✅ |
| `POST` | `/api/demo/reset-circuit-breakers` | Reset CB state (demo-only) | ✅ |

### LoggingApi

| Method | Endpoint | Description | Status |
|--------|----------|-------------|--------|
| `POST` | `/api/logging/log` | Receive log message | ✅ |
| `POST` | `/api/logging/performance` | Receive performance log | ✅ |
| `GET` | `/api/featuretoggle` | Get all feature toggles | ✅ |
| `GET` | `/api/featuretoggle/{name}` | Get specific feature status | ✅ |
| `POST` | `/api/featuretoggle/{name}/toggle` | Toggle feature state | ✅ |
| `GET` | `/loggingHub` | SignalR hub connection | ✅ |

---

## 🛠️ Setup Instructions

### Prerequisites
- **.NET 10 SDK** (upgraded from 8/9)
- Visual Studio 2022 (17.10+) or VS Code
- OpenTelemetry Collector (optional, for centralized tracing)

### 1. Clone Repository
```bash
git clone https://github.com/josefernandoferreiragomes/ResilienceOrderProcessing.git
cd ResilienceOrderProcessing
```

### 2. Project Structure
```
ResilienceOrderProcessing/
├── OrderProcessing.Api/              (Main Minimal API)
│   ├── Endpoints/
│   │   ├── OrderEndpointsBuilder.cs  (Production endpoints)
│   │   └── DemoEndpointsBuilder.cs   (Testing endpoints)
│   ├── Infrastructure/
│   │   └── WebApplicationBuilderExtensions.cs
│   └── Services/
│       └── SignalRLoggingService.cs
│
├── OrderProcessing.Api.Mvc/          (Legacy MVC - Reference only)
│   └── Controllers/
│       └── OrdersController.cs       (Traditional pattern)
│
├── LoggingApi/                       (SignalR + Feature Toggles)
│   ├── Hubs/
│   │   └── LoggingHub.cs
│   └── Services/
│       └── FeatureToggleService.cs
│
├── MockInventoryService.Api/         (External service mock)
├── OrderProcessing.Core/             (Domain models, DTOs, interfaces)
├── OrderProcessing.Infrastructure/   (EF Core, repositories)
├── OrderProcessing.Services/         (Business logic, resilience)
│
└── ResilienceOrderProcessing.ServiceDefaults/
    └── Extensions.cs                 (Aspire service setup)
```

### 3. Build Solution
```bash
dotnet build
```

### 4. Start Services

#### Terminal 1 - Inventory Service (Optional)
```bash
cd MockInventoryService.Api
dotnet run
# Runs on: https://localhost:7261
```

#### Terminal 2 - LoggingApi
```bash
cd LoggingApi
dotnet run
# Runs on: https://localhost:7002
```

#### Terminal 3 - OrderProcessing.Api
```bash
cd OrderProcessing.Api
dotnet run
# Runs on: https://localhost:7103
```

#### Terminal 4 - .NET Aspire Dashboard (Optional)
```bash
dotnet run --project ResilienceOrderProcessing.AppHost
# Provides observability dashboard at http://localhost:18888
```

### 5. Configuration

#### OpenTelemetry Setup
Set environment variable for OTLP export:
```bash
# Windows
set OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317

# Linux/macOS
export OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
```

#### appsettings.json
```json
{
  "Resilience": {
    "RetryPolicy": {
      "MaxRetries": 3,
      "BaseDelayMs": 1000,
      "MaxDelayMs": 10000
    },
    "CircuitBreaker": {
      "FailureThreshold": 3,
      "BreakDuration": 30000
    }
  },
  "LoggingApi": {
    "BaseUrl": "https://localhost:7002"
  }
}
```

---

## 🧪 Testing Guide

### Option 1: HTTP File (REST Client)
```bash
# Install REST Client extension in VS Code
# Open OrderProcessing.Api/test-endpoints.http
# Click "Send Request" above each test
```

### Option 2: Swagger/OpenAPI
```bash
# Navigate to https://localhost:7103
# Use interactive API documentation
```

### Option 3: Postman/Insomnia
Import endpoints from Swagger definition:
```
https://localhost:7103/swagger/v1/swagger.json
```

### Example Requests

#### Create Order
```http
POST https://localhost:7103/api/orders
Content-Type: application/json

{
  "customerId": "cust-12345",
  "items": [
    {
      "productId": "LAPTOP-001",
      "productName": "Test Laptop",
      "quantity": 1,
      "unitPrice": 999.99
    }
  ],
  "paymentMethod": {
    "method": "CreditCard",
    "token": "test-token-123"
  },
  "deliveryAddress": {
    "street": "123 Main St",
    "city": "Boston",
    "state": "MA",
    "zipCode": "02101",
    "country": "USA"
  }
}
```

#### Test Resilience (Sequential)
```http
POST https://localhost:7103/api/demo/test-resilience
Content-Type: application/json

{
  "numberOfOrders": 5,
  "delayBetweenRequestsMs": 500
}
```

#### Simulate Load (Concurrent)
```http
POST https://localhost:7103/api/demo/simulate-load
Content-Type: application/json

{
  "concurrentRequests": 10
}
```

#### Check Feature Toggles
```http
GET https://localhost:7103/api/features
```

#### Toggle Feature
```http
POST https://localhost:7002/api/featuretoggle/RealTimeLogging/toggle
Content-Type: application/json

true
```

---

## 🔧 Implementation Details

### Resilience Architecture

#### Polly Policies Configuration
```csharp
// Per-service timeout strategy
Inventory: 5 seconds
Payment:   10 seconds
Shipping:  8 seconds

// Retry strategy
MaxRetries: 3
BackoffType: Exponential (2^n seconds)
Jitter: Enabled (reduces thundering herd)

// Circuit Breaker
FailureThreshold: 3 consecutive failures
SamplingDuration: 10 seconds
MinimumThroughput: 3 requests
BreakDuration: 30 seconds
```

#### Service Decorators
- `ResilientInventoryService`: Wraps HTTP inventory calls
- `ResilientPaymentService`: Wraps payment processing
- `ResilientShippingService`: Wraps shipping operations

### OpenTelemetry Integration

#### Instrumentations Enabled
- ✅ ASP.NET Core (HTTP requests, gRPC)
- ✅ HTTP Client (outbound calls)
- ✅ Runtime metrics (CPU, memory, GC)
- ✅ Logging (with scopes and context)

#### Exporters
- OTLP (OpenTelemetry Protocol) - for external collectors
- Azure Monitor (optional - requires configuration)

#### Health Check Filtering
- Excludes `/health` and `/alive` endpoints from tracing
- Reduces noise in observability data

### SignalR Real-time Logging

#### Request Lifecycle Logging
1. **Middleware** (`LoggingMiddleware.cs`): Intercepts all requests
2. **Unique RequestId**: Generated per request for correlation
3. **SignalR Broadcast**: Async logging to all connected clients
4. **Graceful Degradation**: API continues if SignalR unavailable

#### Log Severity Levels
- `Information`: Normal operations
- `Warning`: Validation failures, retries
- `Error`: Exceptions, circuit breaker openings

### Feature Toggle System

#### Toggle Types
- `RealTimeLogging`: Enable/disable SignalR logging
- `DetailedErrorLogging`: Include stack traces in logs
- `PerformanceLogging`: Track operation timing metrics

#### Implementation
- Queries LoggingApi for toggle state
- Defaults to `false` if service unavailable
- No API restart required

---

## 📊 Monitoring and Observability

### Health Check Endpoints
- `GET /health` - All checks must pass (readiness)
- `GET /alive` - Only "live" tag checks (liveness)

### Observability Stack
- **Structured Logging**: Serilog with console and file sinks
- **Distributed Tracing**: OpenTelemetry with OTLP export
- **Metrics**: ASP.NET Core, HTTP client, runtime metrics
- **Real-time**: SignalR for instant log visibility
- **Centralized**: .NET Aspire Dashboard integration

### Logging Categories
| Category | Purpose | Example |
|----------|---------|---------|
| Request | HTTP request/response tracking | Method, path, status code |
| Order | Business logic operations | Create, process, cancel |
| Validation | Input validation results | Empty fields, invalid data |
| Performance | Operation timing metrics | Duration, throughput |
| Health | System health status | Service availability |
| Error | Exception tracking | Type, message, stack trace |

---

## 🐳 Docker & Deployment

### Docker Compose (Planned)
```bash
# Note: docker-compose.yml referenced in Next Steps
docker-compose up --build
```

### Service Ports
- **OrderProcessing.Api**: https://localhost:7103
- **LoggingApi**: https://localhost:7002
- **MockInventoryService**: https://localhost:7261
- **Aspire Dashboard**: http://localhost:18888

---

## 📈 Performance Characteristics

### Expected Behavior
- **Successful Request**: 50-150ms (varies by external service)
- **With 1 Retry**: 1-3 seconds (exponential backoff)
- **Circuit Breaker Open**: <10ms (immediate failure)
- **Concurrent Requests** (10): ~1-2 seconds aggregate

### Bottlenecks
- External service calls (Inventory, Payment, Shipping)
- Database operations (Entity Framework)
- SignalR broadcast to clients

### Optimization Opportunities
- Caching for frequently accessed data
- Connection pooling optimization
- Async database queries
- Message queue integration

---

## 🚨 Error Scenarios & Testing

### Simulated Failures
1. **Invalid Input**: Empty CustomerId, zero quantity
2. **Random Failures**: 20% failure rate in cancel operations
3. **Circuit Breaker**: Triggered after 3 consecutive failures
4. **Timeout**: Exceeding per-service timeout limits
5. **Service Unavailable**: Graceful handling when dependencies down

### Testing with Demo Endpoints
```bash
# Test sequential retry behavior
POST /api/demo/test-resilience { "numberOfOrders": 10 }

# Test concurrent load and circuit breaker
POST /api/demo/simulate-load { "concurrentRequests": 50 }

# Monitor failures in SignalR logs
# Watch circuit breaker state transitions
```

---

## ✅ Implementation Status

### Currently Implemented
- ✅ Minimal APIs with complete CRUD
- ✅ Polly resilience patterns (retry, circuit breaker, timeout, bulkhead)
- ✅ SignalR real-time logging
- ✅ OpenTelemetry distributed tracing
- ✅ Health checks (liveness + readiness)
- ✅ Feature toggle integration
- ✅ Request correlation with unique IDs
- ✅ Performance monitoring
- ✅ Demo/testing endpoints
- ✅ .NET 10 upgrade
- ✅ Aspire service defaults integration

### In Progress / Planned
- ⚠️ Dashboard UI (HTML/JavaScript)
- ⚠️ Docker Compose setup
- ⚠️ Production database (SQL Server migration)
- ⚠️ Authentication/Authorization (JWT)
- ⚠️ Rate limiting
- ⚠️ Unit/integration tests
- ⚠️ Message queue integration (Azure Service Bus)
- ⚠️ Caching layer (Redis)

### Not Implemented
- ❌ GraphQL API
- ❌ gRPC services
- ❌ Kubernetes deployment
- ❌ Message saga patterns
- ❌ Event sourcing

---

## 🎯 Quick Start Checklist

- [ ] Clone repository
- [ ] Upgrade to .NET 10 SDK
- [ ] Build solution: `dotnet build`
- [ ] Start LoggingApi: `dotnet run --project LoggingApi`
- [ ] Start OrderProcessing.Api: `dotnet run --project OrderProcessing.Api`
- [ ] Navigate to https://localhost:7103
- [ ] Test endpoints via Swagger UI
- [ ] Monitor logs in real-time
- [ ] Run demo endpoints to test resilience
- [ ] Configure OTEL_EXPORTER_OTLP_ENDPOINT for tracing (optional)

---

## 📚 Architecture Decision Records

### Why Minimal APIs?
- Modern, lightweight pattern
- Dependency injection built-in
- Better testability
- Reduced boilerplate vs MVC controllers
- Aligned with .NET 6+ direction

### Why Polly?
- Industry-standard resilience library
- Comprehensive policy combinations
- Well-tested in production scenarios
- Strong community support

### Why OpenTelemetry?
- Vendor-neutral observability standard
- Industry consolidation around CNCF standards
- Future-proof observability investment
- Aspire framework integration

### Why SignalR?
- Real-time log streaming to clients
- No polling required
- Scalable to multiple concurrent connections
- Built into ASP.NET Core

### Why .NET 10?
- Latest LTS features
- Improved performance
- Enhanced Aspire integration
- Security and reliability improvements

---

## 🤝 Contributing

Contributions welcome! Areas needing help:
1. Dashboard UI implementation
2. Comprehensive unit/integration tests
3. Docker setup automation
4. Performance optimization
5. Documentation improvements

---

## 📚 References

### Official Documentation
- [ASP.NET Core Minimal APIs](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis)
- [Polly Resilience Patterns](https://www.pollydocs.org/)
- [OpenTelemetry .NET](https://opentelemetry.io/docs/instrumentation/net/)
- [SignalR Real-time Communication](https://learn.microsoft.com/en-us/aspnet/core/signalr/introduction)
- [.NET Aspire Service Defaults](https://learn.microsoft.com/en-us/dotnet/aspire/service-defaults)

### Related Articles
- [Circuit Breaker Pattern](https://martinfowler.com/bliki/CircuitBreaker.html)
- [Bulkhead Isolation Pattern](https://docs.microsoft.com/en-us/azure/architecture/patterns/bulkhead)
- [Distributed Tracing](https://opentelemetry.io/docs/concepts/observability-primer/)
- [Health Checks in .NET](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks)

### Tools
- [Postman API Client](https://www.postman.com/)
- [OpenTelemetry Collector](https://opentelemetry.io/docs/collector/)
- [Jaeger Distributed Tracing](https://www.jaegertracing.io/)
- [Grafana Observability](https://grafana.com/)

---

## 📝 License

This project is open source and available under the MIT License.

## 🙋 Support

For issues, questions, or contributions:
- GitHub Issues: https://github.com/josefernandoferreiragomes/ResilienceOrderProcessing/issues
- Discussions: https://github.com/josefernandoferreiragomes/ResilienceOrderProcessing/discussions