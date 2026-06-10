namespace Grimoire.Infrastructure.Persistence.Repository;

using Domain.Common;
using Domain.Common.Repository;
using Domain.Entity;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using Persistence.Database;

public abstract class CrudRepository<T>(ApplicationDbContext context) : IRepository<T> where T : BaseModel, IModel {
	protected ApplicationDbContext Context => context;
	protected DbSet<T> Entities => context.Set<T>();

	public virtual async Task<T?> FindOne(Guid id, CancellationToken cancellationToken = default) =>
		await Entities.AsNoTracking().FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken);

	public virtual async Task<PagedResult<T>> FindAll(int pageIndex, int pageSize, CancellationToken cancellationToken = default) {
		var query = Entities.AsNoTracking().OrderBy(e => e.Id);
		var count = await query.CountAsync(cancellationToken);
		var items = await query.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
		return new PagedResult<T>(items, count, pageIndex, pageSize);
	}

	public async Task<T> Create(T entity, CancellationToken cancellationToken = default) {
		var result = Entities.Add(entity);
		await context.SaveChangesAsync(cancellationToken);
		return result.Entity;
	}

	public async Task<IEnumerable<T>> CreateBulk(IEnumerable<T> entities, CancellationToken cancellationToken = default) {
		var entityList = entities.ToList();
		await Entities.AddRangeAsync(entityList, cancellationToken);
		await context.SaveChangesAsync(cancellationToken);
		return entityList;
	}

	public async Task<T> Update(T entity, CancellationToken cancellationToken = default) {
		var trackedEntry = context.ChangeTracker.Entries<T>()
			.FirstOrDefault(e => e.Entity.Id == entity.Id);
		if (trackedEntry is not null) {
			trackedEntry.State = EntityState.Detached;
		}
		var result = Entities.Update(entity);
		await context.SaveChangesAsync(cancellationToken);
		return result.Entity;
	}

	public async Task<int> Delete(Guid id, CancellationToken cancellationToken = default) => await Entities.Where(entity => entity.Id == id).ExecuteDeleteAsync(cancellationToken);

	public async Task<IEnumerable<T>> Update(IEnumerable<T> entities, CancellationToken cancellationToken = default) {
		var entityList = entities.ToList();
		foreach (var entity in entityList) {
			var trackedEntry = context.ChangeTracker.Entries<T>()
				.FirstOrDefault(e => e.Entity.Id == entity.Id);
			if (trackedEntry is not null) {
				trackedEntry.State = EntityState.Detached;
			}
		}
		Entities.UpdateRange(entityList);
		await context.SaveChangesAsync(cancellationToken);
		return entityList;
	}
}
