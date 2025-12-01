namespace Grimoire.Infrastructure.Persistence.Seeder;

using Microsoft.EntityFrameworkCore;

public class DatabaseSeeder(IEnumerable<ISeeder> seeders) {
	public void SeedAll(DbContext context) {
		foreach (var seeder in seeders) {
			try {
				var result = seeder.SeedData(context);
				if (result) {
					Console.WriteLine($"Successfully seeded data using {seeder.GetType().Name}");
				}
			}
			catch (Exception ex) {
				Console.WriteLine($"Error seeding data using {seeder.GetType().Name}: {ex.Message}");
			}
		}
	}
}
