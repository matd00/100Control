using System.Collections.ObjectModel;
using System.Windows.Input;
using Desktop.Infrastructure.MVVM;
using Domain.Entities;
using Domain.Interfaces.Repositories;

namespace Desktop.Features.Suppliers;

public class SuppliersViewModel : ViewModelBase
{
    private readonly ISupplierRepository _supplierRepository;
    private readonly ISmartSearchService _smartSearchService;

    private string _name = string.Empty;
    private string _contactName = string.Empty;
    private string _email = string.Empty;
    private string _phone = string.Empty;
    private string _document = string.Empty;
    private string _address = string.Empty;
    private string _city = string.Empty;
    private string _state = string.Empty;
    private string _zipCode = string.Empty;
    private SupplierItemViewModel? _selectedSupplier;
    private string _statusMessage = string.Empty;
    private bool _isLoading;
    private bool _isEditing;
    private string _searchText = string.Empty;

    #region Properties

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                FilterSuppliers();
            }
        }
    }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string ContactName
    {
        get => _contactName;
        set => SetProperty(ref _contactName, value);
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

    public SupplierItemViewModel? SelectedSupplier
    {
        get => _selectedSupplier;
        set
        {
            if (SetProperty(ref _selectedSupplier, value) && value != null)
            {
                LoadSupplierToForm(value);
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

    public ObservableCollection<SupplierItemViewModel> AllSuppliers { get; } = new();
    public ObservableCollection<SupplierItemViewModel> Suppliers { get; } = new();

    public ICommand CreateSupplierCommand { get; }
    public ICommand UpdateSupplierCommand { get; }
    public ICommand DeleteSupplierCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand ClearFormCommand { get; }
    public ICommand ClearSearchCommand { get; }
    public ICommand SelectSupplierCommand { get; }

    public SuppliersViewModel(ISupplierRepository supplierRepository, ISmartSearchService smartSearchService)
    {
        _supplierRepository = supplierRepository ?? throw new ArgumentNullException(nameof(supplierRepository));
        _smartSearchService = smartSearchService ?? throw new ArgumentNullException(nameof(smartSearchService));

        CreateSupplierCommand = new AsyncRelayCommand(CreateSupplierAsync, () => !IsLoading && !IsEditing && CanCreate());
        UpdateSupplierCommand = new AsyncRelayCommand(UpdateSupplierAsync, () => !IsLoading && IsEditing && SelectedSupplier != null);
        DeleteSupplierCommand = new AsyncRelayCommand(DeleteSupplierAsync, () => !IsLoading && SelectedSupplier != null);
        RefreshCommand = new AsyncRelayCommand(LoadSuppliersAsync);
        ClearFormCommand = new RelayCommand(ClearForm);
        ClearSearchCommand = new RelayCommand(() => SearchText = string.Empty);
        SelectSupplierCommand = new RelayCommand<SupplierItemViewModel>(SelectSupplier);

        _ = LoadSuppliersAsync();
    }

    private void FilterSuppliers()
    {
        Suppliers.Clear();

        if (string.IsNullOrWhiteSpace(SearchText))
        {
            foreach (var s in AllSuppliers) Suppliers.Add(s);
            return;
        }

        var filtered = _smartSearchService.Search(
            AllSuppliers,
            SearchText,
            s => $"{s.Name} {s.ContactName} {s.Email} {s.Phone} {s.Document} {s.City}",
            threshold: 0.25);

        foreach (var s in filtered)
        {
            Suppliers.Add(s);
        }
    }

    private bool CanCreate() => !string.IsNullOrWhiteSpace(Name) && !string.IsNullOrWhiteSpace(Email);

    private void SelectSupplier(SupplierItemViewModel? supplier)
    {
        if (supplier != null)
        {
            SelectedSupplier = supplier;
            IsEditing = true;
        }
    }

    private void LoadSupplierToForm(SupplierItemViewModel supplier)
    {
        Name = supplier.Name;
        ContactName = supplier.ContactName;
        Email = supplier.Email;
        Phone = supplier.Phone;
        Document = supplier.Document;
        Address = supplier.Address;
        City = supplier.City;
        State = supplier.State;
        ZipCode = supplier.ZipCode;
    }

    private async Task CreateSupplierAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "Criando fornecedor...";

            var supplier = new Supplier(Name, ContactName, Email, Phone, Document);
            await _supplierRepository.SaveAsync(supplier);

            StatusMessage = "✅ Fornecedor criado com sucesso!";
            ClearForm();
            await LoadSuppliersAsync();
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

    private async Task UpdateSupplierAsync()
    {
        if (SelectedSupplier == null) return;

        try
        {
            IsLoading = true;
            StatusMessage = "Atualizando fornecedor...";

            var supplier = await _supplierRepository.GetByIdAsync(SelectedSupplier.Id);
            if (supplier == null)
            {
                StatusMessage = "❌ Fornecedor não encontrado.";
                return;
            }

            supplier.UpdateContact(Email, Phone, ContactName);
            supplier.UpdateAddress(Address, City, State, ZipCode);
            await _supplierRepository.UpdateAsync(supplier);

            StatusMessage = "✅ Fornecedor atualizado com sucesso!";
            ClearForm();
            await LoadSuppliersAsync();
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

    private async Task DeleteSupplierAsync()
    {
        if (SelectedSupplier == null) return;

        try
        {
            IsLoading = true;
            StatusMessage = "Removendo fornecedor...";

            await _supplierRepository.DeleteAsync(SelectedSupplier.Id);

            StatusMessage = "✅ Fornecedor removido com sucesso!";
            ClearForm();
            await LoadSuppliersAsync();
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

    private async Task LoadSuppliersAsync()
    {
        try
        {
            IsLoading = true;
            var suppliers = await _supplierRepository.GetAllAsync();

            AllSuppliers.Clear();
            foreach (var supplier in suppliers.OrderByDescending(s => s.CreatedAt))
            {
                AllSuppliers.Add(new SupplierItemViewModel
                {
                    Id = supplier.Id,
                    Name = supplier.Name,
                    ContactName = supplier.ContactName,
                    Email = supplier.Email,
                    Phone = supplier.Phone,
                    Document = supplier.Document,
                    Address = supplier.Address,
                    City = supplier.City,
                    State = supplier.State,
                    ZipCode = supplier.ZipCode,
                    IsActive = supplier.IsActive
                });
            }
            FilterSuppliers();
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Erro ao carregar fornecedores: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ClearForm()
    {
        Name = string.Empty;
        ContactName = string.Empty;
        Email = string.Empty;
        Phone = string.Empty;
        Document = string.Empty;
        Address = string.Empty;
        City = string.Empty;
        State = string.Empty;
        ZipCode = string.Empty;
        SelectedSupplier = null;
        IsEditing = false;
    }
}

public class SupplierItemViewModel : ViewModelBase
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
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
