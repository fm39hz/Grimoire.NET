namespace Grimoire.Infrastructure.Persistence.Database;

using System.Text.Json;
using Domain.Entity.Book;
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

		var contentComparer = new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<List<Segment>>(
			(c1, c2) => JsonSerializer.Serialize(c1, JsonOptions.Default) ==
						JsonSerializer.Serialize(c2, JsonOptions.Default),
			c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
			c => JsonSerializer.Deserialize<List<Segment>>(JsonSerializer.Serialize(c, JsonOptions.Default),
				JsonOptions.Default)!
			);

		modelBuilder.Entity<ChapterModel>(entity => {
			entity.ToTable("Chapters");
			entity.Property(c => c.Content)
				.HasColumnType("jsonb")
				.HasConversion(
					v => JsonSerializer.Serialize(v, JsonOptions.Default),
					v => JsonSerializer.Deserialize<List<Segment>>(v, JsonOptions.Default) ?? new List<Segment>()
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
						v => JsonSerializer.Deserialize<List<TextSegment>>(v, JsonOptions.Default) ??
							new List<TextSegment>()
						);
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
