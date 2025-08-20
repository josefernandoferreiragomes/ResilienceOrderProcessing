using Microsoft.AspNetCore.Mvc;
using OrderProcessing.Core.DTOs;
using OrderProcessing.Core.Interfaces;
using OrderProcessing.Core.Models;
using System.ComponentModel;

namespace OrderProcessing.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IOrderService orderService, ILogger<OrdersController> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<OrderResponse>> CreateOrder([FromBody] CreateOrderRequest request)
    {
        try
        {
            _logger.LogInformation("Creating new order for customer {CustomerId}", request.CustomerId);

            var order = await _orderService.CreateOrderAsync(request);
            var response = MapToResponse(order);

            return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create order for customer {CustomerId}", request.CustomerId);
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<OrderResponse>> GetOrder(Guid id)
    {
        try
        {
            var order = await _orderService.GetOrderAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            return Ok(MapToResponse(order));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve order {OrderId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("page")]
    public async Task<ActionResult<OrderResponse>> GetOrdersNextPage(
        [FromQuery] int page = 0,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var orders = await _orderService.GetOrdersNextPageAsync(page, pageSize);
            if (orders == null)
            {
                return NotFound();
            }

            return Ok(
                orders.Select(order =>
                    MapToResponse(order)
                    ).ToList()
                );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve orders for page {page}, with {pageCount} items per page", page, pageSize);
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("customer/{customerId}")]
    public async Task<ActionResult<IEnumerable<OrderResponse>>> GetCustomerOrders(string customerId)
    {
        try
        {
            var orders = await _orderService.GetCustomerOrdersAsync(customerId);
            var responses = orders.Select(MapToResponse);

            return Ok(responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve orders for customer {CustomerId}", customerId);
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id}/process")]
    public async Task<ActionResult<OrderResponse>> ProcessOrder(Guid id)
    {
        try
        {
            _logger.LogInformation("Processing order {OrderId}", id);

            var order = await _orderService.ProcessOrderAsync(id);
            var response = MapToResponse(order);

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process order {OrderId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id}/status")]
    public async Task<ActionResult<OrderResponse>> UpdateOrderStatus(
        Guid id,
        [FromBody] UpdateOrderStatusRequest request)
    {
        try
        {
            if (!Enum.TryParse<OrderStatus>(request.Status, true, out var status))
            {
                return BadRequest(new { error = "Invalid order status" });
            }

            var order = await _orderService.UpdateOrderStatusAsync(id, status, request.FailureReason);
            var response = MapToResponse(order);

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update status for order {OrderId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    private static OrderResponse MapToResponse(Order order)
    {
        return new OrderResponse
        {
            Id = order.Id,
            CustomerId = order.CustomerId,
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt,
            Status = order.Status.ToString(),
            TotalAmount = order.TotalAmount,
            FailureReason = order.FailureReason,
            Items = order.Items.Select(item => new OrderItemResponse
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                TotalPrice = item.TotalPrice
            }).ToList(),
            Payment = order.Payment == null ? null : new PaymentInfoResponse
            {
                PaymentId = order.Payment.PaymentId,
                PaymentMethod = order.Payment.PaymentMethod,
                Status = order.Payment.Status.ToString(),
                ProcessedAt = order.Payment.ProcessedAt,
                FailureReason = order.Payment.FailureReason
            },
            Shipping = order.Shipping == null ? null : new ShippingInfoResponse
            {
                TrackingNumber = order.Shipping.TrackingNumber,
                Carrier = order.Shipping.Carrier,
                Status = order.Shipping.Status.ToString(),
                ShippedAt = order.Shipping.ShippedAt,
                EstimatedDelivery = order.Shipping.EstimatedDelivery,
                DeliveryAddress = new AddressResponse
                {
                    Street = order.Shipping.DeliveryAddress.Street,
                    City = order.Shipping.DeliveryAddress.City,
                    State = order.Shipping.DeliveryAddress.State,
                    ZipCode = order.Shipping.DeliveryAddress.ZipCode,
                    Country = order.Shipping.DeliveryAddress.Country
                }
            }
        };
    }
}

public class UpdateOrderStatusRequest
{
    public string Status { get; set; } = string.Empty;
    public string? FailureReason { get; set; }
}