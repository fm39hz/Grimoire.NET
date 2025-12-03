namespace Grimoire.Api.Extension;

using Domain.Common.Repository;
using Infrastructure.Persistence.Repository;
using JetBrains.Annotations;

public static class StorageExtension {
	[UsedImplicitly]
	public static IServiceCollection AddStorage(this IServiceCollection services, WebApplicationBuilder builder) {
		var storageType = builder.Configuration.GetValue<string>("Storage:Type");

		switch (storageType) {
			case "S3":
				// services.AddSingleton<IStorageRepository, S3StorageRepository>();
				throw new NotImplementedException("S3 storage is not implemented yet.");
			case "LocalStorage":
				services.AddSingleton<IStorageRepository, LocalStorageRepository>();
				break;
		}

		return services;
	}
}
