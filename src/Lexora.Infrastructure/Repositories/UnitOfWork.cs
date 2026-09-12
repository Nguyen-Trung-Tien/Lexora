using Lexora.Domain.Interfaces;
using Lexora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Lexora.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly LexoraDbContext _context;

    public UnitOfWork(LexoraDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // Detach pending entries to prevent ChangeTracker poisoning for subsequent operations
            foreach (var entry in _context.ChangeTracker.Entries().ToList())
            {
                entry.State = EntityState.Detached;
            }
            throw;
        }
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
