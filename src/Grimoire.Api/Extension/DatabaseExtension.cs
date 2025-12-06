namespace Grimoire.Api.Extension;

using EntityFramework.Exceptions.PostgreSQL;
using Infrastructure.Configuration;
using Infrastructure.Persistence.Database;
using Infrastructure.Persistence.Seeder;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

public static class DatabaseExtension {
	[UsedImplicitly]
	public static IServiceCollection
		AddDatabaseContext(this IServiceCollection service, WebApplicationBuilder builder) {
		var postgresConnection = new PostgreSqlConfiguration(builder.Configuration);
		builder.Services.AddDbContext<ApplicationDbContext>(options =>
			options.UseNpgsql(postgresConnection.ConnectionString)
				.UseSnakeCaseNamingConvention()
				.UseExceptionProcessor()
				.ConfigureWarnings(w => w.Ignore(CoreEventId.AccidentalEntityType)));
		// service.AddTransient<AdminSeeder>();
		service.AddTransient<DatabaseSeeder>();

		service.AddDbContext<ApplicationDbContext>(optionsBuilder => {
			optionsBuilder.UseNpgsql().UseSnakeCaseNamingConvention();
			// var seeders = new List<ISeeder> {
			// 	new AdminSeeder(userSeed),
			// };
			// var databaseSeeder = new DatabaseSeeder(seeders);
			// optionsBuilder.UseSeeding((context, _) => databaseSeeder.SeedAll(context))
			// 	.UseAsyncSeeding(async (context, _, cancellationToken) =>
			// 		await databaseSeeder.SeedAllAsync(context, cancellationToken));
		});

		return service;
	}
}
