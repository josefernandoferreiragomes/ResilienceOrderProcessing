# Order Processing API with Polly Resilience Patterns

This demo showcases a .NET 8 Order Processing API that implements Polly resilience patterns including retry, circuit breaker, timeout, and monitoring capabilities.

## 🏗️ Architecture

```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   Minimal API   │    │  Order Service   │    │ External APIs   │
│   Controllers   │ -> │  (Business       │ -> │ (Inventory,     │
│                 │    │   Logic)         │    │  Payment,       │
│                 │    │                  │    │  Shipping)      │
└─────────────────┘    └──────────────────┘    └─────────────────┘
                                │
                                ▼
                       ┌──────────────────┐
                       │ Resilient Wrappers│
                       │ - Retry           │
                       │ - Circuit Breaker │
                       │ - Timeout         │
                       │ - Fallback        │
                       └──────────────────┘
```

## 🚀 Getting Started

### Prerequisites
- .NET 8 SDK
- Visual Studio 2022 or VS Code

### Run the Application

```bash
dotnet run --project OrderProcessing.Api
```

Navigate to: `https://localhost:5001/swagger`

## 🔄 Resilience Patterns Implemented

### 1. **Retry Pattern**
- **Max Retries**: 3 attempts
- **Backoff**: Exponential with jitter
- **Base Delay**: 1 second
- **Max Delay**: 10 seconds
- **Handles**: HttpRequestException, TaskCanceledException, TimeoutRejectedException

### 2. **Circuit Breaker Pattern**
- **Failure Threshold**: 3 consecutive failures
- **Sampling Duration**: 10 seconds
- **Break Duration**: 30 seconds
- **Minimum Throughput**: 3 requests

### 3. **Timeout Pattern**
- **Inventory Service**: 5 seconds
- **Payment Service**: 10 seconds
- **Shipping Service**: 8 seconds

### 4. **Fallback Strategies**
- **Inventory**: Assume common products are available
- **Payment**: Return failure with user-friendly message
- **Shipping**: Use default carrier and cost

## 📊 Demo Scenarios

### Basic Order Processing
```bash
POST /api/orders
{
  "customerId": "demo-customer",
  "items": [
    {
      "productId": "LAPTOP-001",
      "productName": "Gaming Laptop",
      "quantity": 1,
      "unitPrice": 1299.99
    }
  ],
  "paymentMethod": { "method": "CreditCard", "token": "demo-token" },
  "deliveryAddress": {
    "street": "123 Demo St",
    "city": "Demo City",
    "state": "DC",
    "zipCode": "12345",
    "country": "USA"
  }
}
```

### Process Order (Triggers Resilience Patterns)
```bash
POST /api/orders/{orderId}/process
```

## 🧪 Testing Resilience Patterns

### 1. Test Multiple Orders with Resilience
```bash
POST /api/demo/test-resilience
{
  "numberOfOrders": 20,
  "delayBetweenRequestsMs": 100
}
```

### 2. Simulate High Load (Circuit Breaker)
```bash
POST /api/demo/simulate-load
{
  "concurrentRequests": 50
}
```

### 3. Monitor Circuit Breaker Status
```bash
GET /api/monitoring/circuit-breakers
GET /api/monitoring/resilience-summary
```

## 📈 Monitoring Endpoints

| Endpoint | Description |
|----------|-------------|
| `GET /api/monitoring/circuit-breakers` | All circuit breaker statuses |
| `GET /api/monitoring/circuit-breakers/{service}` | Specific service status |
| `GET /api/monitoring/resilience-summary` | Overall resilience summary |
| `GET /health` | Health check including resilience |

## 🎯 Expected Behavior

### Normal Operation
- Orders process successfully
- Circuit breakers remain closed
- Minimal retries

### Under Stress
1. **Transient Failures**: Retry policy kicks in (3 attempts)
2. **Persistent Failures**: Circuit breaker opens after 3 failures
3. **Fallback Activation**: Fallback responses when circuit is open
4. **Recovery**: Circuit breaker closes when service recovers

### Failure Rates in Mock Services
- **Inventory**: 10% failure rate + 5% timeout
- **Payment**: 15% failure rate + 10% timeout
- **Shipping**: 10% failure rate + 5% timeout

## 📝 Sample Test Flow

1. **Start the API**
2. **Create baseline orders** - Should succeed
3. **Run resilience test** - Observe retries in logs
4. **Simulate load** - Watch circuit breakers open
5. **Monitor status** - Check `/api/monitoring/circuit-breakers`
6. **Wait for recovery** - Circuit breakers close automatically
7. **Test again** - Should work normally

## 🔍 Observability

### Logs
- Structured logging with Serilog
- Retry attempts logged with reasons
- Circuit breaker state changes
- Fallback activations

### Metrics Available
- Circuit breaker states
- Retry counts and reasons
- Timeout occurrences
- Success/failure rates
- Response times

## 🚦 Health Checks

The API includes comprehensive health checks:
- Database connectivity
- Resilience pattern health
- Circuit breaker status
- Service availability

Access at: `GET /health`

## 🔧 Configuration

Resilience patterns are fully configurable via `appsettings.json`:

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
      "SamplingDurationMs": 10000,
      "BreakDurationMs": 30000
    }
  }
}
```

## 🎉 Future Extensions

This demo is designed for easy extension with:
- SignalR for real-time order status updates
- Feature toggles for logging destinations
- Custom logging database
- Additional resilience patterns (bulkhead, rate limiting)

## 🐛 Troubleshooting

- **High failure rates**: Increase retry counts or circuit breaker thresholds
- **Long timeouts**: Adjust timeout values in configuration
- **Circuit breakers stuck open**: Check service health and wait for break duration
- **Performance issues**: Monitor concurrent request limits

## 📚 Key Polly Concepts Demonstrated

1. **Resilience Pipelines**: Combining multiple patterns
2. **Policy Composition**: Retry + Circuit Breaker + Timeout
3. **Fallback Strategies**: Graceful degradation
4. **Monitoring**: Circuit breaker state tracking
5. **Configuration**: Flexible policy configuration