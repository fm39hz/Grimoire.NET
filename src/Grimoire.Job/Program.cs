using Grimoire.Job;

var host = Host.CreateDefaultBuilder(args)
	.ConfigureServices(static (context, services) => services.AddJobInfrastructure(context.Configuration))
	.ConfigureLogging(static (context, logging) => {
		logging.ClearProviders();
		logging.AddConsole();
		logging.AddConfiguration(context.Configuration.GetSection("Logging"));
	})
	.Build();

await host.RunAsync();
