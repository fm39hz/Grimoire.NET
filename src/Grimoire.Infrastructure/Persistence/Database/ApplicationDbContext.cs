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

	protected override void OnModelCreating(ModelBuilder modelBuilder) {
		base.OnModelCreating(modelBuilder);

		modelBuilder.Entity<SeriesModel>(entity => {
			entity.Property(s => s.Title)
				.HasMaxLength(500)
				.IsRequired();
			
			entity.Property(s => s.Metadata)
				.HasColumnType("jsonb")
				.HasConversion(
					v => JsonSerializer.Serialize(v, JsonConfiguration.JsonOptions),
					v => JsonSerializer.Deserialize<SeriesMetadata>(v, JsonConfiguration.JsonOptions) ??
						new SeriesMetadata()
					)
				.Metadata.SetValueComparer(JsonConfiguration.MetadataComparer);
			entity.HasIndex(s => s.Metadata).HasMethod("gin");
			entity.HasIndex(s => s.Title).IsUnique();
		});

		modelBuilder.Entity<VolumeModel>(entity => {
			entity.Property(v => v.Title)
				.HasMaxLength(500)
				.IsRequired();
			
			entity.HasIndex(e => new { e.SeriesId, e.Order });
			entity.OwnsOne(s => s.Metadata, metaBuilder => {
				metaBuilder.ToJson();
			});
		});

		modelBuilder.Entity<ChapterModel>(entity => {
			entity.Property(c => c.Title)
				.HasMaxLength(500)
				.IsRequired();
			
			entity.HasIndex(e => new { e.VolumeId, e.Order });
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

		modelBuilder.Entity<AssetModel>(entity => {
			entity.Property(a => a.Path)
				.HasMaxLength(1000)
				.IsRequired();
			
			entity.Property(a => a.FileHash)
				.HasMaxLength(64)
				.IsRequired();
			
			entity.Property(a => a.RefType)
				.HasMaxLength(50)
				.IsRequired();
			
			entity.HasIndex(a => a.SeriesId);
			entity.HasIndex(a => a.FileHash);
			entity.HasIndex(a => new { a.SeriesId, a.FileHash });
			entity.HasIndex(a => a.RefType);
		});
	}
}
