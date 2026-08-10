namespace Grimoire.Application;

using Export;
using Import;
using Ingestion.Analysis;
using Ingestion.Reconciliation;
using Ingestion.Legacy;
using Ingestion.Execution;
using Ingestion.Research;
using Mapper;
using Microsoft.Extensions.DependencyInjection;
using Publish.Export;
using Publish.Export.Steps;
using Publish.Import;
using Publish.Import.Steps;
using Service.Contract;
using Service.Implementation;
using Service.Pipeline.Ingestion;
using Service.Pipeline.Ingestion.Steps;
using Service.Pipeline.Publishing;
using Service.Pipeline.Publishing.Steps;
using Service.Strategy;

public static class DependencyInjection {
	public static IServiceCollection AddApplication(this IServiceCollection services) {
		// Register services
		services.AddScoped<ISeriesService, SeriesService>();
		services.AddScoped<IVolumeService, VolumeService>();
		services.AddScoped<IChapterService, ChapterService>();
		services.AddScoped<IBinderyService, BinderyService>();
		services.AddScoped<IAssetService, AssetService>();
		services.AddScoped<IAssetOwnershipService, AssetOwnershipService>();
		services.AddScoped<ISeriesSyncService, SeriesSyncService>();
		services.AddScoped<ISeriesRevisionService, SeriesRevisionService>();
		services.AddScoped<IBookRestructureService, BookRestructureService>();
		services.AddScoped<ISegmentService, SegmentService>();
		services.AddScoped<IStorageService, StorageService>();
		services.AddScoped<IBookTreeService, BookTreeService>();
		services.AddScoped<ISeriesNodeService>(static sp => sp.GetRequiredService<IBookTreeService>());
		services.AddScoped<IVolumeNodeService>(static sp => sp.GetRequiredService<IBookTreeService>());
		services.AddScoped<IChapterNodeService>(static sp => sp.GetRequiredService<IBookTreeService>());
		services.AddScoped<INodeManagerService>(static sp => sp.GetRequiredService<IBookTreeService>());
		services.AddScoped<NodeMatchScorer>();
		services.AddScoped<SiblingSequenceAligner>();
		services.AddScoped<SourcePackagePlanBuilder>();
		services.AddScoped<IImportAnalysisService, ImportAnalysisService>();
		services.AddScoped<ISeriesTargetResolver, SeriesTargetResolver>();
		services.AddScoped<ILegacySourcePackageAdapter, LegacySourcePackageAdapter>();
		services.AddScoped<ILegacyShadowAnalyzer, LegacyShadowAnalyzer>();
		services.AddScoped<IImportExecutionService, ImportExecutionService>();
		services.AddScoped<IResearchCoordinator, ResearchCoordinator>();

		// Register mappers
		services.AddScoped<IBookMapper, BookMapper>();

		// Register export collaborators
		services.AddScoped<VolumeResolver>();
		services.AddScoped<ChapterLoader>();
		services.AddScoped<CoverResolver>();
		services.AddScoped<ImageAssetCollector>();
		services.AddScoped<BookExportOrchestrator>();

		// Register ingestion strategies in priority order
		services.AddScoped<IIngestionStrategy, PreProcessedIngestionStrategy>();
		services.AddScoped<IIngestionStrategy, RawMarkdownIngestionStrategy>();
		services.AddScoped<IIngestionStrategyFactory, IngestionStrategyFactory>();

		// Register import strategies
		services.AddScoped<IImportStrategy, EpubImportStrategy>();
		services.AddScoped<ImportStrategyFactory>();

		// Register import collaborators
		services.AddScoped<IVolumeTreeResolver, VolumeTreeResolver>();
		services.AddScoped<IChapterImportHandler, ChapterImportHandler>();
		services.AddScoped<IMediaImportService, MediaImportService>();

		// Register Export Pipeline and Steps
		services.AddScoped<IExportPipeline, ExportPipeline>();
		services.AddScoped<IExportPipelineStep, RequestValidationStep>();
		services.AddScoped<IExportPipelineStep, DeduplicationStep>();
		services.AddScoped<IExportPipelineStep, ContentGenerationStep>();
		services.AddScoped<IExportPipelineStep, StorageUploadStep>();
		services.AddScoped<IExportPipelineStep, DatabaseRecordStep>();

		// Register Import Pipeline and Steps
		services.AddScoped<IImportPipeline, ImportPipeline>();
		services.AddScoped<IImportPipelineStep, ParseImportStep>();
		services.AddScoped<IImportPipelineStep, MetadataResolutionStep>();
		services.AddScoped<IImportPipelineStep, LegacyImportShadowStep>();
		services.AddScoped<IImportPipelineStep, MediaUploadStep>();
		services.AddScoped<IImportPipelineStep, VolumeTreeResolutionStep>();
		services.AddScoped<IImportPipelineStep, ChapterImportStep>();
		services.AddScoped<IImportPipelineStep, ReconcileOwnershipStep>();
		services.AddScoped<IImportPipelineStep, LegacyImportShadowOutcomeStep>();

		// Register Ingestion Pipeline
		services.AddScoped<IngestionCoordinator>();
		services.AddScoped<IIngestionPipelineStep, ParseContentStep>();
		services.AddScoped<IIngestionPipelineStep, PersistenceStep>();
		services.AddScoped<IIngestionPipelineStep, LcaOwnershipStep>();

		// Register Publishing Pipeline
		services.AddScoped<PublishingCoordinator>();
		services.AddScoped<IPublishingPipelineStep, BuildContextStep>();
		services.AddScoped<IPublishingPipelineStep, SerializationStep>();

		return services;
	}
}
