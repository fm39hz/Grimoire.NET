namespace Grimoire.Infrastructure.Persistence.Seeder;

using Microsoft.EntityFrameworkCore;

public interface ISeeder {
	public bool SeedData(DbContext context);
	public Task<bool> SeedDataAsync(DbContext context, CancellationToken cancellationToken = default);
}
