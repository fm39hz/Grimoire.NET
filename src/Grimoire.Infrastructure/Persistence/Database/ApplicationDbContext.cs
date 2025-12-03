namespace Grimoire.Infrastructure.Persistence.Database;

using System.Text.Json;
using Configuration;
using Domain.Entity.Book;
using Domain.Entity.Book.Metadata;
using Domain.Entity.Book.Segment;
using EntityFramework.Exceptions.PostgreSQL;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options) {
	[UsedImplicitly] public DbSet<SeriesModel> Series { get; set; } = null!;
	[UsedImplicitly] public DbSet<VolumeModel> Volumes { get; set; } = null!;
	[UsedImplicitly] public DbSet<ChapterModel> Chapters { get; set; } = null!;
	[UsedImplicitly] public DbSet<AssetModel> Assets { get; set; } = null!;
	[UsedImplicitly] public DbSet<ChapterVariantModel> ChapterVariants { get; set; } = null!; // Added DbSet

	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
		base.OnConfiguring(optionsBuilder.ConfigureWarnings(w => w.Ignore(CoreEventId.AccidentalEntityType)));
		optionsBuilder.UseNpgsql().UseSnakeCaseNamingConvention().UseExceptionProcessor();
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder) {
		base.OnModelCreating(modelBuilder);

		modelBuilder.Entity<SeriesModel>(entity => {
			entity.Property(s => s.Metadata)
				.HasColumnType("jsonb")
				.HasConversion(
					v => JsonSerializer.Serialize(v, JsonConfiguration.JsonOptions),
					v => JsonSerializer.Deserialize<SeriesMetadata>(v, JsonConfiguration.JsonOptions) ??
						new SeriesMetadata()
					)
				.Metadata.SetValueComparer(JsonConfiguration.MetadataComparer);
			entity.HasIndex(s => s.Metadata)
				.HasMethod("gin");
			entity.HasIndex(s => s.Title).IsUnique();
		});

		modelBuilder.Entity<VolumeModel>(entity => {
			entity.OwnsOne(s => s.Metadata, metaBuilder => {
				metaBuilder.ToJson();
			});
		});

		modelBuilder.Entity<ChapterModel>(entity => {
			entity.HasMany(c => c.Variants)
				.WithOne(cv => cv.Chapter)
				.HasForeignKey(cv => cv.ChapterId)
				.OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<ChapterVariantModel>(entity => {
			entity.Property(cv => cv.Content)
				.HasColumnType("jsonb")
				.HasConversion(
					v => JsonSerializer.Serialize(v, JsonConfiguration.JsonOptions),
					v => JsonSerializer.Deserialize<List<SegmentModel>>(v, JsonConfiguration.JsonOptions) ??
						new List<SegmentModel>()
					).Metadata.SetValueComparer(JsonConfiguration.ContentComparer);
			entity.Property(cv => cv.Footnotes)
				.HasColumnType("jsonb")
				.HasConversion(
					v => JsonSerializer.Serialize(v, JsonConfiguration.JsonOptions),
					v =>
						JsonSerializer.Deserialize<List<FootnoteSegmentModel>>(
							v, JsonConfiguration.JsonOptions) ??
						new List<FootnoteSegmentModel>()
					).Metadata.SetValueComparer(JsonConfiguration.FootnoteComparer);
		});
	}
}
