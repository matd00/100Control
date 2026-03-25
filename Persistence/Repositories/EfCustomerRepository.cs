using Domain.Entities;
using Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Persistence.Repositories;

public class EfCustomerRepository : ICustomerRepository
{
    private readonly PaintballManagerDbContext _context;

    public EfCustomerRepository(PaintballManagerDbContext context)
    {
        _context = context;
    }

    public async Task<Customer?> GetByIdAsync(Guid id)
    {
        return await _context.Customers.FindAsync(id);
    }

    public async Task<Customer?> GetByEmailAsync(string email)
    {
        return await _context.Customers.FirstOrDefaultAsync(c => c.Email == email.ToLowerInvariant());
    }

    public async Task<IEnumerable<Customer>> GetAllAsync()
    {
        return await _context.Customers.ToListAsync();
    }

    public async Task<IEnumerable<Customer>> GetActiveAsync()
    {
        return await _context.Customers.Where(c => c.IsActive).ToListAsync();
    }

    public async Task SaveAsync(Customer customer)
    {
        var existing = await _context.Customers.FindAsync(customer.Id);
        if (existing == null)
        {
            _context.Customers.Add(customer);
        }
        else
        {
            _context.Entry(existing).CurrentValues.SetValues(customer);
        }
    }

    public async Task UpdateAsync(Customer customer)
    {
        _context.Customers.Update(customer);
    }

    public async Task DeleteAsync(Guid id)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer != null)
        {
            _context.Customers.Remove(customer);
        }
    }
}
