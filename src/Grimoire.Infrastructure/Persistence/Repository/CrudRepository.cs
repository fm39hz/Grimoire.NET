namespace Grimoire.Infrastructure.Persistence.Repository;

using Domain.Common.Repository;
using Domain.Entity;
using Microsoft.EntityFrameworkCore;

public abstract class CrudRepository<T>(DbContext context) : IRepository<T> where T : BaseModel, IModel {
	protected DbSet<T> Entities => context.Set<T>();

	public virtual async Task<T?> FindOne(Guid id) =>
		await Entities.AsNoTracking().FirstOrDefaultAsync(entity => entity.Id == id);

	public virtual async Task<IEnumerable<T>> FindAll(int pageIndex, int pageSize) {
		var items = await Entities.AsNoTracking()
			.OrderBy(e => e.Id)
			.Skip((pageIndex - 1) * pageSize)
			.Take(pageSize)
			.ToListAsync();

		return items;
	}

	public virtual async Task<int> CountAll() => await Entities.AsNoTracking().CountAsync();

	public async Task<T> Create(T entity) {
		var result = Entities.Add(entity);
		await context.SaveChangesAsync();
		return result.Entity;
	}

	public async Task<IEnumerable<T>> CreateBulk(IEnumerable<T> entities) {
		var entityList = entities.ToList();
		await Entities.AddRangeAsync(entityList);
		await context.SaveChangesAsync();
		return entityList;
	}

	public async Task<T> Update(T entity) {
		entity.MarkAsUpdated();
		var result = Entities.Update(entity);
		await context.SaveChangesAsync();
		return result.Entity;
	}

	public async Task<int> Delete(Guid id) => await Entities.Where(entity => entity.Id == id).ExecuteDeleteAsync();

	public async Task<IEnumerable<T>> Update(IEnumerable<T> entities) {
		var baseModels = entities.ToList();

		foreach (var entity in baseModels) {
			entity.MarkAsUpdated();
		}

		Entities.UpdateRange(baseModels);
		await context.SaveChangesAsync();
		return baseModels;
	}
}
