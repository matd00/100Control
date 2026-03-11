using Desktop.Infrastructure.MVVM;
using Domain.Entities;
using System.Collections.ObjectModel;

namespace Desktop.Features.Orders;

public class FactoryOrderViewModel : ViewModelBase
{
    private Guid _id;
    private string _customerName = string.Empty;
    private string _customerContact = string.Empty;
    private string _deliveryAddress = string.Empty;
    private string _supplierName = string.Empty;
    private string _supplierContact = string.Empty;
    private decimal _totalCost;
    private decimal _totalSalePrice;
    private decimal _margin;
    private FactoryOrderStatus _status;
    private OrderSource _channel;
    private string? _trackingCode;
    private string? _notes;
    private DateTime _createdAt;
    private DateTime? _updatedAt;

    public Guid Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    public string CustomerName
    {
        get => _customerName;
        set => SetProperty(ref _customerName, value);
    }

    public string CustomerContact
    {
        get => _customerContact;
        set => SetProperty(ref _customerContact, value);
    }

    public string DeliveryAddress
    {
        get => _deliveryAddress;
        set => SetProperty(ref _deliveryAddress, value);
    }

    public string SupplierName
    {
        get => _supplierName;
        set => SetProperty(ref _supplierName, value);
    }

    public string SupplierContact
    {
        get => _supplierContact;
        set => SetProperty(ref _supplierContact, value);
    }

    public decimal TotalCost
    {
        get => _totalCost;
        set => SetProperty(ref _totalCost, value);
    }

    public decimal TotalSalePrice
    {
        get => _totalSalePrice;
        set => SetProperty(ref _totalSalePrice, value);
    }

    public decimal Margin
    {
        get => _margin;
        set => SetProperty(ref _margin, value);
    }

    public FactoryOrderStatus Status
    {
        get => _status;
        set
        {
            if (SetProperty(ref _status, value))
            {
                OnPropertyChanged(nameof(StatusText));
                OnPropertyChanged(nameof(StatusColor));
            }
        }
    }

    public OrderSource Channel
    {
        get => _channel;
        set => SetProperty(ref _channel, value);
    }

    public string? TrackingCode
    {
        get => _trackingCode;
        set
        {
            if (SetProperty(ref _trackingCode, value))
            {
                OnPropertyChanged(nameof(HasTrackingCode));
            }
        }
    }

    public string? Notes
    {
        get => _notes;
        set => SetProperty(ref _notes, value);
    }

    public DateTime CreatedAt
    {
        get => _createdAt;
        set
        {
            if (SetProperty(ref _createdAt, value))
            {
                OnPropertyChanged(nameof(CreatedAtFormatted));
            }
        }
    }

    public DateTime? UpdatedAt
    {
        get => _updatedAt;
        set => SetProperty(ref _updatedAt, value);
    }

    public ObservableCollection<FactoryOrderItemViewModel> Items { get; } = new();

    // UI Helpers
    public string CreatedAtFormatted => CreatedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
    public string TotalCostFormatted => string.Format("R$ {0:N2}", TotalCost);
    public string TotalSalePriceFormatted => string.Format("R$ {0:N2}", TotalSalePrice);
    public string MarginFormatted => string.Format("{0:N2}%", Margin);
    public bool HasTrackingCode => !string.IsNullOrWhiteSpace(TrackingCode);

    public string StatusText => Status switch
    {
        FactoryOrderStatus.Pendente => "Pendente",
        FactoryOrderStatus.Confirmado => "Confirmado",
        FactoryOrderStatus.EnviadoPelaFabrica => "Enviado a Fabrica",
        FactoryOrderStatus.Entregue => "Entregue",
        FactoryOrderStatus.Cancelado => "Cancelado",
        _ => Status.ToString()
    };

    public string StatusColor => Status switch
    {
        FactoryOrderStatus.Pendente => "#94A3B8", // Cinza
        FactoryOrderStatus.Confirmado => "#3B82F6", // Azul
        FactoryOrderStatus.EnviadoPelaFabrica => "#F59E0B", // Amarelo
        FactoryOrderStatus.Entregue => "#22C55E", // Verde
        FactoryOrderStatus.Cancelado => "#EF4444", // Vermelho
        _ => "#000000"
    };
    
    public string StatusBackgroundColor => Status switch
    {
        FactoryOrderStatus.Pendente => "#F1F5F9", 
        FactoryOrderStatus.Confirmado => "#DBEAFE", 
        FactoryOrderStatus.EnviadoPelaFabrica => "#FEF3C7", 
        FactoryOrderStatus.Entregue => "#DCFCE7", 
        FactoryOrderStatus.Cancelado => "#FEE2E2", 
        _ => "#FFFFFF"
    };

    public int ItemsCount => Items.Count;
    public bool IsNew => Id == Guid.Empty;

    public void LoadFromEntity(FactoryOrder order)
    {
        Id = order.Id;
        CustomerName = order.CustomerName;
        CustomerContact = order.CustomerContact;
        DeliveryAddress = order.DeliveryAddress;
        SupplierName = order.SupplierName;
        SupplierContact = order.SupplierContact;
        TotalCost = order.TotalCost;
        TotalSalePrice = order.TotalSalePrice;
        Margin = order.Margin;
        Status = order.Status;
        Channel = order.Channel;
        TrackingCode = order.TrackingCode;
        Notes = order.Notes;
        CreatedAt = order.CreatedAt;
        UpdatedAt = order.UpdatedAt;

        Items.Clear();
        foreach (var item in order.Items)
        {
            Items.Add(new FactoryOrderItemViewModel
            {
                Id = item.Id,
                Description = item.Description,
                Quantity = item.Quantity,
                UnitCost = item.UnitCost,
                UnitSalePrice = item.UnitSalePrice,
                SubtotalCost = item.SubtotalCost,
                SubtotalSalePrice = item.SubtotalSalePrice
            });
        }
        
        OnPropertyChanged(nameof(ItemsCount));
    }
}

public class FactoryOrderItemViewModel : ViewModelBase
{
    private Guid _id;
    private string _description = string.Empty;
    private int _quantity;
    private decimal _unitCost;
    private decimal _unitSalePrice;
    private decimal _subtotalCost;
    private decimal _subtotalSalePrice;

    public Guid Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    public int Quantity
    {
        get => _quantity;
        set
        {
            if (SetProperty(ref _quantity, value))
            {
                Recalculate();
            }
        }
    }

    public decimal UnitCost
    {
        get => _unitCost;
        set
        {
            if (SetProperty(ref _unitCost, value))
            {
                Recalculate();
            }
        }
    }

    public decimal UnitSalePrice
    {
        get => _unitSalePrice;
        set
        {
            if (SetProperty(ref _unitSalePrice, value))
            {
                Recalculate();
            }
        }
    }

    public decimal SubtotalCost
    {
        get => _subtotalCost;
        set => SetProperty(ref _subtotalCost, value);
    }

    public decimal SubtotalSalePrice
    {
        get => _subtotalSalePrice;
        set => SetProperty(ref _subtotalSalePrice, value);
    }

    public string UnitCostFormatted => string.Format("R$ {0:N2}", UnitCost);
    public string UnitSalePriceFormatted => string.Format("R$ {0:N2}", UnitSalePrice);
    public string SubtotalCostFormatted => string.Format("R$ {0:N2}", SubtotalCost);
    public string SubtotalSalePriceFormatted => string.Format("R$ {0:N2}", SubtotalSalePrice);

    private void Recalculate()
    {
        SubtotalCost = Quantity * UnitCost;
        SubtotalSalePrice = Quantity * UnitSalePrice;
        OnPropertyChanged(nameof(SubtotalCostFormatted));
        OnPropertyChanged(nameof(SubtotalSalePriceFormatted));
    }
}

public class CustomerForFactoryOrderViewModel : ViewModelBase
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullAddress { get; set; } = string.Empty;
    public string Document { get; set; } = string.Empty;
    public string DisplayText => $"{Name} — {Phone}";
}
