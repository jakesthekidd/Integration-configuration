using Microsoft.EntityFrameworkCore;
using Transflo.Platform.Transformer.Core.Data;
using Transflo.Platform.Transformer.Core.Models;

namespace Transflo.Platform.Transformer.Core.Repositories;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(string id);
    Task<List<Customer>> GetAllAsync(bool? activeOnly = null);
    Task<Customer> CreateAsync(Customer customer);
    Task<Customer> UpdateAsync(Customer customer);
    Task DeleteAsync(string id);
}

public class CustomerRepository : ICustomerRepository
{
    private readonly FieldMappingDbContext _context;
    private readonly ILogger<CustomerRepository> _logger;

    public CustomerRepository(FieldMappingDbContext context, ILogger<CustomerRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Customer?> GetByIdAsync(string id)
    {
        return await _context.Customers.FindAsync(id);
    }

    public async Task<List<Customer>> GetAllAsync(bool? activeOnly = null)
    {
        var query = _context.Customers.AsQueryable();

        if (activeOnly.HasValue)
            query = query.Where(c => c.IsActive == activeOnly.Value);

        return await query
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<Customer> CreateAsync(Customer customer)
    {
        customer.CreatedAt = DateTime.UtcNow;
        customer.UpdatedAt = DateTime.UtcNow;

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created customer: {Name} (ID: {Id})", customer.Name, customer.Id);
        return customer;
    }

    public async Task<Customer> UpdateAsync(Customer customer)
    {
        customer.UpdatedAt = DateTime.UtcNow;

        _context.Customers.Update(customer);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated customer: {Id}", customer.Id);
        return customer;
    }

    public async Task DeleteAsync(string id)
    {
        var customer = await GetByIdAsync(id);
        if (customer != null)
        {
            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Deleted customer: {Id}", id);
        }
    }
}
