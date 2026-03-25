using Domain.Entities;
using Domain.Interfaces.Repositories;
using Domain.Interfaces;
using Domain.Common;

namespace Application.UseCases.Customers;

public class RegisterCustomerUseCase
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterCustomerUseCase(ICustomerRepository customerRepository, IUnitOfWork unitOfWork)
    {
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Execute(RegisterCustomerCommand command)
    {
        try
        {
            if (command == null)
                return Result<Guid>.Failure("Command cannot be null");

            if (string.IsNullOrWhiteSpace(command.Name))
                return Result<Guid>.Failure("Nome do cliente é obrigatório");

            // Validar nome completo (nome + sobrenome) - necessário para gerar etiqueta de frete
            var nameParts = command.Name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (nameParts.Length < 2)
                return Result<Guid>.Failure("Informe o nome completo (nome e sobrenome). Exemplo: 'João Silva'");

            if (string.IsNullOrWhiteSpace(command.Email))
                return Result<Guid>.Failure("Email é obrigatório");

            if (string.IsNullOrWhiteSpace(command.Phone))
                return Result<Guid>.Failure("Telefone é obrigatório");

            if (string.IsNullOrWhiteSpace(command.Document))
                return Result<Guid>.Failure("CPF/CNPJ é obrigatório");

            var customer = new Customer(
                command.Name,
                command.Email,
                command.Phone,
                command.Document
            );

            if (!string.IsNullOrEmpty(command.Address))
            {
                customer.UpdateFullAddress(
                    command.Address,
                    command.Number ?? string.Empty,
                    command.Complement ?? string.Empty,
                    command.District ?? string.Empty,
                    command.City ?? string.Empty,
                    command.State ?? string.Empty,
                    command.ZipCode ?? string.Empty
                );
            }

            await _customerRepository.SaveAsync(customer);
            await _unitOfWork.SaveChangesAsync();

            return Result<Guid>.Success(customer.Id);
        }
        catch (ArgumentException ex)
        {
            return Result<Guid>.Failure(ex.Message);
        }
        catch (Exception)
        {
            return Result<Guid>.Failure("An error occurred while registering the customer.");
        }
    }
}

public class RegisterCustomerCommand
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Document { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Number { get; set; } = string.Empty;
    public string Complement { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
}
