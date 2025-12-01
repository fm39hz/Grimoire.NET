namespace Grimoire.Infrastructure.Configuration;

using Microsoft.Extensions.Configuration;

public sealed record PostgreSqlConfiguration {
	public PostgreSqlConfiguration(IConfiguration configuration) {
		ConnectionString = configuration["ConnectionStrings:Postgre"]!;
	}

	public string ConnectionString { get; } = string.Empty;
}
