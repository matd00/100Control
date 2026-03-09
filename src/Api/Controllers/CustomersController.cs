using Microsoft.AspNetCore.Mvc;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Application.UseCases.Customers;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerRepository _customerRepository;
    private readonly RegisterCustomerUseCase _registerCustomerUseCase;

    public CustomersController(ICustomerRepository customerRepository, RegisterCustomerUseCase registerCustomerUseCase)
    {
        _customerRepository = customerRepository;
        _registerCustomerUseCase = registerCustomerUseCase;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CustomerDto>>> GetAll()
    {
        var customers = await _customerRepository.GetAllAsync();
        return Ok(customers.Select(c => new CustomerDto(c)));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CustomerDto>> GetById(Guid id)
    {
        var customer = await _customerRepository.GetByIdAsync(id);
        if (customer == null)
            return NotFound();
        return Ok(new CustomerDto(customer));
    }

    [HttpGet("email/{email}")]
    public async Task<ActionResult<CustomerDto>> GetByEmail(string email)
    {
        var customer = await _customerRepository.GetByEmailAsync(email);
        if (customer == null)
            return NotFound();
        return Ok(new CustomerDto(customer));
    }

    [HttpPost]
    public async Task<ActionResult<CustomerDto>> Create([FromBody] CreateCustomerRequest request)
    {
        try
        {
            var command = new RegisterCustomerCommand
            {
                Name = request.Name,
                Email = request.Email,
                Phone = request.Phone,
                Document = request.Document
            };

            var result = await _registerCustomerUseCase.Execute(command);

            if (!result.Success)
                return BadRequest(new { error = result.ErrorMessage });

            var customer = await _customerRepository.GetByIdAsync(result.CustomerId);
            if (customer == null)
                return BadRequest(new { error = "Customer created but not found" });

            return CreatedAtAction(nameof(GetById), new { id = result.CustomerId }, new CustomerDto(customer));
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<CustomerDto>> Update(Guid id, [FromBody] UpdateCustomerRequest request)
    {
        var customer = await _customerRepository.GetByIdAsync(id);
        if (customer == null)
            return NotFound();

        try
        {
            customer.UpdateContactInfo(request.Name, request.Email, request.Phone);
            customer.UpdateAddress(request.Address, request.City, request.State, request.ZipCode);
            
            await _customerRepository.UpdateAsync(customer);
            return Ok(new CustomerDto(customer));
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        var customer = await _customerRepository.GetByIdAsync(id);
        if (customer == null)
            return NotFound();

        await _customerRepository.DeleteAsync(id);
        return NoContent();
    }
}

public record CustomerDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string Document { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string ZipCode { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }

    public CustomerDto() { }
    public CustomerDto(Customer customer)
    {
        Id = customer.Id;
        Name = customer.Name;
        Email = customer.Email;
        Phone = customer.Phone;
        Document = customer.Document;
        Address = customer.Address;
        City = customer.City;
        State = customer.State;
        ZipCode = customer.ZipCode;
        IsActive = customer.IsActive;
        CreatedAt = customer.CreatedAt;
    }
}

public record CreateCustomerRequest
{
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string Document { get; init; } = string.Empty;
}

public record UpdateCustomerRequest
{
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string ZipCode { get; init; } = string.Empty;
}
