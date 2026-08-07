namespace Grimoire.Domain.Common.Repository;

using System.Threading;

/// <summary>
///     Unit of Work pattern for managing database transactions
/// </summary>
public interface IUnitOfWork {
	/// <summary>
	///     Begins a new database transaction
	/// </summary>
	public Task BeginTransactionAsync(CancellationToken cancellationToken = default);

	/// <summary>
	///     Commits the current transaction
	/// </summary>
	public Task CommitTransactionAsync(CancellationToken cancellationToken = default);

	/// <summary>
	///     Rolls back the current transaction
	/// </summary>
	public Task RollbackTransactionAsync(CancellationToken cancellationToken = default);

	/// <summary>
	///     Saves all pending changes to the database
	/// </summary>
	public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

	/// <summary>
	///     Detaches all tracked entities from the change tracker without discarding
	///     the transaction. Long-lived workflows that save many entities in one outer
	///     transaction (bulk import) must call this after each batch; otherwise the
	///     tracker accumulates every entity and each later save is O(n²).
	/// </summary>
	public void DetachTrackedEntities();

	/// <summary>
	///     Registers an action to run after the transaction successfully commits
	/// </summary>
	public void RegisterPostCommitAction(Func<Task> action);
}
