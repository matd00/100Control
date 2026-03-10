namespace Domain.Entities;

public class Customer
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public string Address { get; private set; } = string.Empty;
    public string Number { get; private set; } = string.Empty;
    public string Complement { get; private set; } = string.Empty;
    public string District { get; private set; } = string.Empty;
    public string City { get; private set; } = string.Empty;
    public string State { get; private set; } = string.Empty;
    public string ZipCode { get; private set; } = string.Empty;
    public string Document { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // EF Core constructor
    private Customer() { }

    public Customer(string name, string email, string phone, string document)
    {
        // Security: Input validation
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Customer name cannot be empty", nameof(name));

        if (name.Length > 200)
            throw new ArgumentException("Customer name cannot exceed 200 characters", nameof(name));

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty", nameof(email));

        if (email.Length > 200)
            throw new ArgumentException("Email cannot exceed 200 characters", nameof(email));

        if (!IsValidEmail(email))
            throw new ArgumentException("Invalid email format", nameof(email));

        if (string.IsNullOrWhiteSpace(phone))
            throw new ArgumentException("Phone cannot be empty", nameof(phone));

        if (phone.Length > 20)
            throw new ArgumentException("Phone cannot exceed 20 characters", nameof(phone));

        if (string.IsNullOrWhiteSpace(document))
            throw new ArgumentException("Document cannot be empty", nameof(document));

        if (document.Length > 20)
            throw new ArgumentException("Document cannot exceed 20 characters", nameof(document));

        Id = Guid.NewGuid();
        Name = name.Trim();
        Email = email.Trim().ToLowerInvariant();
        Phone = phone.Trim();
        Document = document.Trim();
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    public void UpdateContact(string email, string phone)
    {
        Email = email;
        Phone = phone;
    }

    public void UpdateContactInfo(string name, string email, string phone)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Customer name cannot be empty", nameof(name));

        Name = name.Trim();
        Email = email?.Trim().ToLowerInvariant() ?? Email;
        Phone = phone?.Trim() ?? Phone;
    }

    public void UpdateAddress(string address, string city, string state, string zipCode)
    {
        Address = address;
        City = city;
        State = state;
        ZipCode = zipCode;
    }

    public void UpdateFullAddress(string address, string number, string complement, string district, string city, string state, string zipCode)
    {
        Address = address ?? string.Empty;
        Number = number ?? string.Empty;
        Complement = complement ?? string.Empty;
        District = district ?? string.Empty;
        City = city ?? string.Empty;
        State = state ?? string.Empty;
        ZipCode = zipCode ?? string.Empty;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}
