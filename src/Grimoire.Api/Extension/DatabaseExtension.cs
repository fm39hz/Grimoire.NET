namespace Grimoire.Api.Extension;

using EntityFramework.Exceptions.PostgreSQL;
using Infrastructure.Configuration;
using Infrastructure.Persistence.Database;
using Infrastructure.Persistence.Seeder;
using Grimoire.Domain.Entity.Book;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

public static class DatabaseExtension {
	[UsedImplicitly]
	public static IServiceCollection
		AddDatabaseContext(this IServiceCollection service, WebApplicationBuilder builder) {
		var connectionString = builder.Configuration["ConnectionStrings:Postgre"]!;
		var postgresConnection = new PostgreSqlConfiguration(connectionString);
		builder.Services.AddDbContext<ApplicationDbContext>(options =>
			options.UseNpgsql(postgresConnection.ConnectionString, o => {
				o.MapEnum<BookNodeType>();
				o.MapEnum<ChapterStatus>();
			})
				.UseSnakeCaseNamingConvention()
				.UseExceptionProcessor()
				.AddInterceptors(new AuditInterceptor())
				.ConfigureWarnings(w => w.Ignore(CoreEventId.AccidentalEntityType)));

		service.AddTransient<DatabaseSeeder>();

		return service;
	}
}
