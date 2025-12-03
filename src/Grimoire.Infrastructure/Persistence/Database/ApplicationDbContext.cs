namespace Grimoire.Infrastructure.Persistence.Database;

using System.Text.Json;
using Configuration;
using Domain.Entity.Book;
using Domain.Entity.Book.Metadata;
using Domain.Entity.Book.Segment;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options) {
	[UsedImplicitly] public DbSet<SeriesModel> Series { get; set; } = null!;
	[UsedImplicitly] public DbSet<VolumeModel> Volumes { get; set; } = null!;
	[UsedImplicitly] public DbSet<ChapterModel> Chapters { get; set; } = null!;
	[UsedImplicitly] public DbSet<AssetModel> Assets { get; set; } = null!;

	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
		base.OnConfiguring(optionsBuilder.ConfigureWarnings(w => w.Ignore(CoreEventId.AccidentalEntityType)));

	protected override void OnModelCreating(ModelBuilder modelBuilder) {
		base.OnModelCreating(modelBuilder);

		modelBuilder.Entity<SeriesModel>(entity => {
			entity.ToTable("Series");
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
			entity.HasIndex(s => s.Title);
		});

		modelBuilder.Entity<VolumeModel>(entity => {
			entity.ToTable("Volumes");
			entity.OwnsOne(s => s.Metadata, metaBuilder => {
				metaBuilder.ToJson();
			});
		});

		modelBuilder.Entity<ChapterModel>(entity => {
			entity.ToTable("Chapters");
			entity.Property(c => c.Content)
				.HasColumnType("jsonb")
				.HasConversion(
					v => JsonSerializer.Serialize(v, JsonConfiguration.JsonOptions),
					v => JsonSerializer.Deserialize<List<SegmentModel>>(v, JsonConfiguration.JsonOptions) ??
						new List<SegmentModel>()
					).Metadata.SetValueComparer(JsonConfiguration.ContentComparer);
			entity.Property(c => c.Footnotes)
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
