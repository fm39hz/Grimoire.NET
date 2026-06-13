namespace Grimoire.Infrastructure.Persistence.Repository;

using System.Threading;
using Database;
using Domain.Common.Repository;
using Microsoft.EntityFrameworkCore.Storage;

public sealed class UnitOfWork(ApplicationDbContext context) : IUnitOfWork {
	private IDbContextTransaction? _currentTransaction;
	private int _transactionCount;

	public async Task BeginTransactionAsync(CancellationToken cancellationToken = default) {
		if (_currentTransaction == null) {
			_currentTransaction = await context.Database.BeginTransactionAsync(cancellationToken);
		}
		_transactionCount++;
	}

	public async Task CommitTransactionAsync(CancellationToken cancellationToken = default) {
		if (_currentTransaction == null) {
			throw new InvalidOperationException("No transaction is in progress");
		}

		_transactionCount--;
		if (_transactionCount == 0) {
			try {
				await context.SaveChangesAsync(cancellationToken);
				await _currentTransaction.CommitAsync(cancellationToken);
			}
			catch {
				await _currentTransaction.RollbackAsync(cancellationToken);
				throw;
			}
			finally {
				_currentTransaction.Dispose();
				_currentTransaction = null;
			}
		}
	}

	public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default) {
		if (_currentTransaction == null) {
			return;
		}

		_transactionCount = 0;
		try {
			await _currentTransaction.RollbackAsync(cancellationToken);
		}
		finally {
			_currentTransaction.Dispose();
			_currentTransaction = null;
		}
	}

	public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => await context.SaveChangesAsync(cancellationToken);
}

