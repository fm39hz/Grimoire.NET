namespace Grimoire.Domain.Common.Repository;

/// <summary>
///     Unit of Work pattern for managing database transactions
/// </summary>
public interface IUnitOfWork {
	/// <summary>
	///     Begins a new database transaction
	/// </summary>
	public Task BeginTransactionAsync();

	/// <summary>
	///     Commits the current transaction
	/// </summary>
	public Task CommitTransactionAsync();

	/// <summary>
	///     Rolls back the current transaction
	/// </summary>
	public Task RollbackTransactionAsync();

	/// <summary>
	///     Saves all pending changes to the database
	/// </summary>
	public Task<int> SaveChangesAsync();
}
