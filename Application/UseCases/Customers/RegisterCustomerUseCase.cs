using Domain.Entities;
using Domain.Interfaces.Repositories;

namespace Application.UseCases.Customers;

public class RegisterCustomerUseCase
{
    private readonly ICustomerRepository _customerRepository;

    public RegisterCustomerUseCase(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<RegisterCustomerResult> Execute(RegisterCustomerCommand command)
    {
        try
        {
            if (command == null)
                return RegisterCustomerResult.Failure("Command cannot be null");

            if (string.IsNullOrWhiteSpace(command.Name))
                return RegisterCustomerResult.Failure("Customer name is required");

            if (string.IsNullOrWhiteSpace(command.Email))
                return RegisterCustomerResult.Failure("Email is required");

            if (string.IsNullOrWhiteSpace(command.Phone))
                return RegisterCustomerResult.Failure("Phone is required");

            if (string.IsNullOrWhiteSpace(command.Document))
                return RegisterCustomerResult.Failure("Document is required");

            var customer = new Customer(
                command.Name,
                command.Email,
                command.Phone,
                command.Document
            );

            if (!string.IsNullOrEmpty(command.Address))
            {
                customer.UpdateAddress(
                    command.Address,
                    command.City ?? string.Empty,
                    command.State ?? string.Empty,
                    command.ZipCode ?? string.Empty
                );
            }

            await _customerRepository.SaveAsync(customer);

            return RegisterCustomerResult.SuccessResult(customer.Id);
        }
        catch (ArgumentException ex)
        {
            return RegisterCustomerResult.Failure(ex.Message);
        }
        catch (Exception)
        {
            return RegisterCustomerResult.Failure("An error occurred while registering the customer.");
        }
    }
}

public class RegisterCustomerResult
{
    public bool Success { get; private set; }
    public string ErrorMessage { get; private set; } = string.Empty;
    public Guid CustomerId { get; private set; }

    public static RegisterCustomerResult SuccessResult(Guid customerId) => new() { Success = true, CustomerId = customerId };
    public static RegisterCustomerResult Failure(string error) => new() { Success = false, ErrorMessage = error };
}

public class RegisterCustomerCommand
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Document { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
}
