namespace Grimoire.Infrastructure.Configuration;

public sealed record PostgreSqlConfiguration {
	public PostgreSqlConfiguration(string connectionString) {
		ConnectionString = connectionString;
	}

	public string ConnectionString { get; } = string.Empty;
}
