namespace Grimoire.Infrastructure.Persistence.Database;

using System.Text.Json;
using Domain.Entity.Book;
using Domain.Entity.Book.Segment;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
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

		var contentComparer = new ValueComparer<List<SegmentModel>>(
			(c1, c2) => JsonSerializer.Serialize(c1, JsonOptions.Default) ==
						JsonSerializer.Serialize(c2, JsonOptions.Default),
			c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
			c => JsonSerializer.Deserialize<List<SegmentModel>>(JsonSerializer.Serialize(c, JsonOptions.Default),
				JsonOptions.Default)!
			);
		var descriptionComparer = new ValueComparer<List<TextSegmentModel>>(
			(c1, c2) => JsonSerializer.Serialize(c1, JsonOptions.Default) ==
						JsonSerializer.Serialize(c2, JsonOptions.Default),
			c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
			c => JsonSerializer.Deserialize<List<TextSegmentModel>>(JsonSerializer.Serialize(c, JsonOptions.Default),
				JsonOptions.Default)!
			);
		modelBuilder.Entity<ChapterModel>(entity => {
			entity.ToTable("Chapters");
			entity.Property(c => c.Content)
				.HasColumnType("jsonb")
				.HasConversion(
					v => JsonSerializer.Serialize(v, JsonOptions.Default),
					v => JsonSerializer.Deserialize<List<SegmentModel>>(v, JsonOptions.Default) ??
						new List<SegmentModel>()
					)
				.Metadata.SetValueComparer(contentComparer);
		});

		modelBuilder.Entity<SeriesModel>(entity => {
			entity.ToTable("Series");
			entity.OwnsOne(s => s.Metadata, metaBuilder => {
				metaBuilder.ToJson();
				metaBuilder.Property(m => m.Description)
					.HasConversion(
						v => JsonSerializer.Serialize(v, JsonOptions.Default),
						v => JsonSerializer.Deserialize<List<TextSegmentModel>>(v, JsonOptions.Default) ??
							new List<TextSegmentModel>()
						).Metadata.SetValueComparer(descriptionComparer);
			});
		});
		modelBuilder.Entity<VolumeModel>(entity => {
			entity.ToTable("Volumes");
			entity.OwnsOne(s => s.Metadata, metaBuilder => {
				metaBuilder.ToJson();
			});
		});
	}
}
