using Microsoft.AspNetCore.Mvc;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Application.UseCases.Orders;
using MediatR;
using Application.Features.Orders.Commands;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IProductRepository _productRepository;
    private readonly IMediator _mediator;
    private readonly UpdateOrderStatusUseCase _updateOrderStatusUseCase;

    public OrdersController(
        IOrderRepository orderRepository,
        ICustomerRepository customerRepository,
        IProductRepository productRepository,
        IMediator mediator,
        UpdateOrderStatusUseCase updateOrderStatusUseCase)
    {
        _orderRepository = orderRepository;
        _customerRepository = customerRepository;
        _productRepository = productRepository;
        _mediator = mediator;
        _updateOrderStatusUseCase = updateOrderStatusUseCase;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetAll()
    {
        var orders = await _orderRepository.GetAllAsync();
        return Ok(orders.Select(o => new OrderDto(o)));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<OrderDto>> GetById(Guid id)
    {
        var order = await _orderRepository.GetByIdAsync(id);
        if (order == null)
            return NotFound();
        return Ok(new OrderDto(order));
    }

    [HttpGet("customer/{customerId}")]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetByCustomer(Guid customerId)
    {
        var orders = await _orderRepository.GetByCustomerIdAsync(customerId);
        return Ok(orders.Select(o => new OrderDto(o)));
    }

    [HttpGet("pending")]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetPending()
    {
        var orders = await _orderRepository.GetAllAsync();
        var pendingOrders = orders.Where(o => o.Status == OrderStatus.Pending);
        return Ok(pendingOrders.Select(o => new OrderDto(o)));
    }

    [HttpPost]
    public async Task<ActionResult<OrderDto>> Create([FromBody] CreateOrderRequest request)
    {
        try
        {
            var customer = await _customerRepository.GetByIdAsync(request.CustomerId);
            if (customer == null)
                return BadRequest(new { error = "Customer not found" });

            var order = new Order(request.CustomerId, request.Source);
            await _orderRepository.SaveAsync(order);

            return CreatedAtAction(nameof(GetById), new { id = order.Id }, new OrderDto(order));
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id}/items")]
    public async Task<ActionResult<OrderDto>> AddItem(Guid id, [FromBody] AddOrderItemRequest request)
    {
        var order = await _orderRepository.GetByIdAsync(id);
        if (order == null)
            return NotFound();

        var product = await _productRepository.GetByIdAsync(request.ProductId);
        if (product == null)
            return BadRequest(new { error = "Product not found" });

        try
        {
            order.AddItem(request.ProductId, request.Quantity, product.Price);
            await _orderRepository.UpdateAsync(order);
            return Ok(new OrderDto(order));
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id}/process")]
    public async Task<ActionResult<OrderDto>> Process(Guid id)
    {
        var order = await _orderRepository.GetByIdAsync(id);
        if (order == null)
            return NotFound();

        try
        {
            order.MarkAsProcessing();
            await _orderRepository.UpdateAsync(order);
            return Ok(new OrderDto(order));
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id}/complete")]
    public async Task<ActionResult<OrderDto>> Complete(Guid id)
    {
        var order = await _orderRepository.GetByIdAsync(id);
        if (order == null)
            return NotFound();

        try
        {
            order.MarkAsCompleted();
            await _orderRepository.UpdateAsync(order);
            return Ok(new OrderDto(order));
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{id}/cancel")]
    public async Task<ActionResult<OrderDto>> Cancel(Guid id)
    {
        var order = await _orderRepository.GetByIdAsync(id);
        if (order == null)
            return NotFound();

        try
        {
            order.Cancel();
            await _orderRepository.UpdateAsync(order);
            return Ok(new OrderDto(order));
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        var order = await _orderRepository.GetByIdAsync(id);
        if (order == null)
            return NotFound();

        await _orderRepository.DeleteAsync(id);
        return NoContent();
    }
}

public record OrderDto
{
    public Guid Id { get; init; }
    public Guid CustomerId { get; init; }
    public string Status { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public int ItemsCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public List<OrderItemDto> Items { get; init; } = new();

    public OrderDto() { }
    public OrderDto(Order order)
    {
        Id = order.Id;
        CustomerId = order.CustomerId;
        Status = order.Status.ToString();
        Source = order.Source.ToString();
        TotalAmount = order.TotalAmount;
        ItemsCount = order.Items.Count;
        CreatedAt = order.CreatedAt;
        Items = order.Items.Select(i => new OrderItemDto(i)).ToList();
    }
}

public record OrderItemDto
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal Subtotal { get; init; }

    public OrderItemDto() { }
    public OrderItemDto(OrderItem item)
    {
        Id = item.Id;
        ProductId = item.ProductId;
        Quantity = item.Quantity;
        UnitPrice = item.UnitPrice;
        Subtotal = item.Subtotal;
    }
}

public record CreateOrderRequest
{
    public Guid CustomerId { get; init; }
    public OrderSource Source { get; init; } = OrderSource.Direct;
}

public record AddOrderItemRequest
{
    public Guid ProductId { get; init; }
    public int Quantity { get; init; } = 1;
}
