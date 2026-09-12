using Lexora.Domain.Interfaces;
using Lexora.Infrastructure.Data;

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
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
