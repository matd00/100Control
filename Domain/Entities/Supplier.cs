using Domain.Common;

namespace Domain.Entities;

public class Supplier : Entity
{
    public string Name { get; private set; } = string.Empty;
    public string ContactName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public string Address { get; private set; } = string.Empty;
    public string City { get; private set; } = string.Empty;
    public string State { get; private set; } = string.Empty;
    public string ZipCode { get; private set; } = string.Empty;
    public string Document { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // EF Core constructor
    private Supplier() { }

    public Supplier(string name, string contactName, string email, string phone, string document)
    {
        // Security: Input validation
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Supplier name cannot be empty", nameof(name));

        if (name.Length > 200)
            throw new ArgumentException("Supplier name cannot exceed 200 characters", nameof(name));

        if (string.IsNullOrWhiteSpace(contactName))
            throw new ArgumentException("Contact name cannot be empty", nameof(contactName));

        if (contactName.Length > 200)
            throw new ArgumentException("Contact name cannot exceed 200 characters", nameof(contactName));

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
        ContactName = contactName.Trim();
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

    public void UpdateContact(string email, string phone, string contactName)
    {
        // Security: Input validation
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty", nameof(email));

        if (!IsValidEmail(email))
            throw new ArgumentException("Invalid email format", nameof(email));

        if (string.IsNullOrWhiteSpace(phone))
            throw new ArgumentException("Phone cannot be empty", nameof(phone));

        if (phone.Length > 20)
            throw new ArgumentException("Phone cannot exceed 20 characters", nameof(phone));

        if (string.IsNullOrWhiteSpace(contactName))
            throw new ArgumentException("Contact name cannot be empty", nameof(contactName));

        if (contactName.Length > 200)
            throw new ArgumentException("Contact name cannot exceed 200 characters", nameof(contactName));

        if (!IsActive)
            throw new InvalidOperationException("Cannot update inactive supplier");

        Email = email.Trim().ToLowerInvariant();
        Phone = phone.Trim();
        ContactName = contactName.Trim();
    }

    public void UpdateAddress(string address, string city, string state, string zipCode)
    {
        // Security: Input validation
        if (string.IsNullOrWhiteSpace(address))
            throw new ArgumentException("Address cannot be empty", nameof(address));

        if (address.Length > 500)
            throw new ArgumentException("Address cannot exceed 500 characters", nameof(address));

        if (string.IsNullOrWhiteSpace(city))
            throw new ArgumentException("City cannot be empty", nameof(city));

        if (city.Length > 100)
            throw new ArgumentException("City cannot exceed 100 characters", nameof(city));

        if (string.IsNullOrWhiteSpace(state))
            throw new ArgumentException("State cannot be empty", nameof(state));

        if (state.Length > 2)
            throw new ArgumentException("State must be 2 characters", nameof(state));

        if (string.IsNullOrWhiteSpace(zipCode))
            throw new ArgumentException("Zip code cannot be empty", nameof(zipCode));

        if (zipCode.Length > 10)
            throw new ArgumentException("Zip code cannot exceed 10 characters", nameof(zipCode));

        if (!IsActive)
            throw new InvalidOperationException("Cannot update inactive supplier");

        Address = address.Trim();
        City = city.Trim();
        State = state.Trim().ToUpperInvariant();
        ZipCode = zipCode.Trim();
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}
