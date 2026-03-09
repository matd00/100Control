using System.Collections.ObjectModel;
using System.Windows.Input;
using Desktop.Infrastructure.MVVM;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Application.UseCases.Customers;

namespace Desktop.Features.Customers;

public class CustomersViewModel : ViewModelBase
{
    private readonly ICustomerRepository _customerRepository;
    private readonly RegisterCustomerUseCase _registerCustomerUseCase;

    private string _name = string.Empty;
    private string _email = string.Empty;
    private string _phone = string.Empty;
    private string _document = string.Empty;
    private string _address = string.Empty;
    private string _city = string.Empty;
    private string _state = string.Empty;
    private string _zipCode = string.Empty;
    private CustomerItemViewModel? _selectedCustomer;
    private string _statusMessage = string.Empty;
    private bool _isLoading;
    private bool _isEditing;

    #region Properties

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    public string Phone
    {
        get => _phone;
        set => SetProperty(ref _phone, value);
    }

    public string Document
    {
        get => _document;
        set => SetProperty(ref _document, value);
    }

    public string Address
    {
        get => _address;
        set => SetProperty(ref _address, value);
    }

    public string City
    {
        get => _city;
        set => SetProperty(ref _city, value);
    }

    public string State
    {
        get => _state;
        set => SetProperty(ref _state, value);
    }

    public string ZipCode
    {
        get => _zipCode;
        set => SetProperty(ref _zipCode, value);
    }

    public CustomerItemViewModel? SelectedCustomer
    {
        get => _selectedCustomer;
        set
        {
            if (SetProperty(ref _selectedCustomer, value) && value != null)
            {
                LoadCustomerToForm(value);
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public bool IsEditing
    {
        get => _isEditing;
        set => SetProperty(ref _isEditing, value);
    }

    #endregion

    public ObservableCollection<CustomerItemViewModel> Customers { get; } = new();

    public ICommand CreateCustomerCommand { get; }
    public ICommand UpdateCustomerCommand { get; }
    public ICommand DeleteCustomerCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand ClearFormCommand { get; }
    public ICommand SelectCustomerCommand { get; }

    public CustomersViewModel(ICustomerRepository customerRepository, RegisterCustomerUseCase registerCustomerUseCase)
    {
        _customerRepository = customerRepository ?? throw new ArgumentNullException(nameof(customerRepository));
        _registerCustomerUseCase = registerCustomerUseCase ?? throw new ArgumentNullException(nameof(registerCustomerUseCase));

        CreateCustomerCommand = new AsyncRelayCommand(CreateCustomerAsync, () => !IsLoading && !IsEditing && CanCreate());
        UpdateCustomerCommand = new AsyncRelayCommand(UpdateCustomerAsync, () => !IsLoading && IsEditing && SelectedCustomer != null);
        DeleteCustomerCommand = new AsyncRelayCommand(DeleteCustomerAsync, () => !IsLoading && SelectedCustomer != null);
        RefreshCommand = new AsyncRelayCommand(LoadCustomersAsync);
        ClearFormCommand = new RelayCommand(ClearForm);
        SelectCustomerCommand = new RelayCommand<CustomerItemViewModel>(SelectCustomer);

        _ = LoadCustomersAsync();
    }

    private bool CanCreate() => !string.IsNullOrWhiteSpace(Name) && !string.IsNullOrWhiteSpace(Email);

    private void SelectCustomer(CustomerItemViewModel? customer)
    {
        if (customer != null)
        {
            SelectedCustomer = customer;
            IsEditing = true;
        }
    }

    private void LoadCustomerToForm(CustomerItemViewModel customer)
    {
        Name = customer.Name;
        Email = customer.Email;
        Phone = customer.Phone;
        Document = customer.Document;
        Address = customer.Address;
        City = customer.City;
        State = customer.State;
        ZipCode = customer.ZipCode;
    }

    private async Task CreateCustomerAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Criando cliente...";

            var command = new RegisterCustomerCommand
            {
                Name = Name,
                Email = Email,
                Phone = Phone,
                Document = Document,
                Address = Address,
                City = City,
                State = State,
                ZipCode = ZipCode
            };

            var result = await _registerCustomerUseCase.Execute(command);

            if (!result.Success)
            {
                StatusMessage = $"❌ Erro: {result.ErrorMessage}";
                return;
            }

            StatusMessage = "✅ Cliente criado com sucesso!";
            ClearForm();
            await LoadCustomersAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Erro: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task UpdateCustomerAsync()
    {
        if (SelectedCustomer == null) return;

        try
        {
            IsLoading = true;
            StatusMessage = "Atualizando cliente...";

            var customer = await _customerRepository.GetByIdAsync(SelectedCustomer.Id);
            if (customer == null)
            {
                StatusMessage = "❌ Cliente não encontrado.";
                return;
            }

            customer.UpdateContact(Email, Phone);
            customer.UpdateAddress(Address, City, State, ZipCode);
            await _customerRepository.UpdateAsync(customer);

            StatusMessage = "✅ Cliente atualizado com sucesso!";
            ClearForm();
            await LoadCustomersAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Erro: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task DeleteCustomerAsync()
    {
        if (SelectedCustomer == null) return;

        try
        {
            IsLoading = true;
            StatusMessage = "Removendo cliente...";

            await _customerRepository.DeleteAsync(SelectedCustomer.Id);

            StatusMessage = "✅ Cliente removido com sucesso!";
            ClearForm();
            await LoadCustomersAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Erro: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadCustomersAsync()
    {
        try
        {
            IsLoading = true;
            var customers = await _customerRepository.GetAllAsync();

            Customers.Clear();
            foreach (var customer in customers.OrderByDescending(c => c.CreatedAt))
            {
                Customers.Add(new CustomerItemViewModel
                {
                    Id = customer.Id,
                    Name = customer.Name,
                    Email = customer.Email,
                    Phone = customer.Phone,
                    Document = customer.Document,
                    Address = customer.Address,
                    City = customer.City,
                    State = customer.State,
                    ZipCode = customer.ZipCode,
                    IsActive = customer.IsActive
                });
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Erro ao carregar clientes: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ClearForm()
    {
        Name = string.Empty;
        Email = string.Empty;
        Phone = string.Empty;
        Document = string.Empty;
        Address = string.Empty;
        City = string.Empty;
        State = string.Empty;
        ZipCode = string.Empty;
        SelectedCustomer = null;
        IsEditing = false;
    }
}

public class CustomerItemViewModel : ViewModelBase
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Document { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public bool IsActive { get; set; }

    public string Location => !string.IsNullOrEmpty(City) ? $"{City}, {State}" : "Não informado";
}
