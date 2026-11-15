namespace Lexora.Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IMemoryTipRepository MemoryTips { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
    void ClearTracker();
}
