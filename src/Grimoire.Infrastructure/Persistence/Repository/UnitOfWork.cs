namespace Grimoire.Infrastructure.Persistence.Repository;

using System.Threading;
using Database;
using Domain.Common.Repository;
using Microsoft.EntityFrameworkCore.Storage;

public sealed class UnitOfWork(ApplicationDbContext context) : IUnitOfWork {
	private IDbContextTransaction? _currentTransaction;

	public async Task BeginTransactionAsync(CancellationToken cancellationToken = default) {
		if (_currentTransaction != null) {
			throw new InvalidOperationException("A transaction is already in progress");
		}

		_currentTransaction = await context.Database.BeginTransactionAsync(cancellationToken);
	}

	public async Task CommitTransactionAsync(CancellationToken cancellationToken = default) {
		if (_currentTransaction == null) {
			throw new InvalidOperationException("No transaction is in progress");
		}

		try {
			await context.SaveChangesAsync(cancellationToken);
			await _currentTransaction.CommitAsync(cancellationToken);
		}
		catch {
			await RollbackTransactionAsync();
			throw;
		}
		finally {
			_currentTransaction.Dispose();
			_currentTransaction = null;
		}
	}

	public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default) {
		if (_currentTransaction == null) {
			return;
		}

		await _currentTransaction.RollbackAsync(cancellationToken);
		_currentTransaction.Dispose();
		_currentTransaction = null;
	}

	public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => await context.SaveChangesAsync(cancellationToken);
}
