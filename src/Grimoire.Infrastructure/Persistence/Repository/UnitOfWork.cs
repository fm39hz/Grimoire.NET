namespace Grimoire.Infrastructure.Persistence.Repository;

using Database;
using Domain.Common.Repository;
using Microsoft.EntityFrameworkCore.Storage;

public sealed class UnitOfWork(ApplicationDbContext context) : IUnitOfWork {
	private IDbContextTransaction? _currentTransaction;

	public async Task BeginTransactionAsync() {
		if (_currentTransaction != null) {
			throw new InvalidOperationException("A transaction is already in progress");
		}

		_currentTransaction = await context.Database.BeginTransactionAsync();
	}

	public async Task CommitTransactionAsync() {
		if (_currentTransaction == null) {
			throw new InvalidOperationException("No transaction is in progress");
		}

		try {
			await context.SaveChangesAsync();
			await _currentTransaction.CommitAsync();
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

	public async Task RollbackTransactionAsync() {
		if (_currentTransaction == null) {
			return;
		}

		await _currentTransaction.RollbackAsync();
		_currentTransaction.Dispose();
		_currentTransaction = null;
	}

	public async Task<int> SaveChangesAsync() => await context.SaveChangesAsync();
}
