namespace Grimoire.Api.Extension;

using Domain.Common.Repository;
using Infrastructure.Configuration;
using Infrastructure.Persistence.Repository;
using JetBrains.Annotations;

public static class StorageExtension {
	[UsedImplicitly]
	public static IServiceCollection AddStorage(this IServiceCollection services, WebApplicationBuilder builder) {
		services.Configure<StorageConfiguration>(
			builder.Configuration.GetSection(StorageConfiguration.SectionName));
		
		var storageType = builder.Configuration.GetValue<string>("Storage:Type");

		switch (storageType) {
			case "S3":
				// services.AddSingleton<IStorageRepository, S3StorageRepository>();
				throw new NotImplementedException("S3 storage is not implemented yet.");
			case "LocalStorage":
				services.AddScoped<IStorageRepository, LocalStorageRepository>();
				break;
		}

		return services;
	}
}
