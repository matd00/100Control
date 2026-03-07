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

    public async Task Execute(RegisterCustomerCommand command)
    {
        try
        {
            // Security: Input validation
            if (command == null)
                throw new ArgumentNullException(nameof(command));

            if (string.IsNullOrWhiteSpace(command.Name))
                throw new ArgumentException("Customer name is required", nameof(command.Name));

            if (string.IsNullOrWhiteSpace(command.Email))
                throw new ArgumentException("Email is required", nameof(command.Email));

            if (string.IsNullOrWhiteSpace(command.Phone))
                throw new ArgumentException("Phone is required", nameof(command.Phone));

            if (string.IsNullOrWhiteSpace(command.Document))
                throw new ArgumentException("Document is required", nameof(command.Document));

            var customer = new Customer(
                command.Name,
                command.Email,
                command.Phone,
                command.Document
            );

            if (!string.IsNullOrEmpty(command.Address))
            {
                if (string.IsNullOrWhiteSpace(command.City) || 
                    string.IsNullOrWhiteSpace(command.State) || 
                    string.IsNullOrWhiteSpace(command.ZipCode))
                    throw new ArgumentException("Complete address information is required");

                customer.UpdateAddress(
                    command.Address,
                    command.City,
                    command.State,
                    command.ZipCode
                );
            }

            await _customerRepository.SaveAsync(customer);
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Security: Don't expose internal exception details
            throw new InvalidOperationException("An error occurred while registering the customer. Please try again later.");
        }
    }
}

public class RegisterCustomerCommand
{
    public string Name { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public string Document { get; set; }
    public string Address { get; set; }
    public string City { get; set; }
    public string State { get; set; }
    public string ZipCode { get; set; }
}
