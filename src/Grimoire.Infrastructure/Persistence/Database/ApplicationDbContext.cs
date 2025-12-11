namespace Grimoire.Infrastructure.Persistence.Database;

using System.Text.Json;
using Configuration;
using Domain.Entity.Book;
using Domain.Entity.Book.Metadata;
using Domain.Entity.Book.Segment;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options) {
	[UsedImplicitly] public DbSet<SeriesModel> Series { get; set; } = null!;
	[UsedImplicitly] public DbSet<VolumeModel> Volumes { get; set; } = null!;
	[UsedImplicitly] public DbSet<ChapterModel> Chapters { get; set; } = null!;
	[UsedImplicitly] public DbSet<ChapterContentModel> ChapterContents { get; set; } = null!;
	[UsedImplicitly] public DbSet<GlossaryTerm> GlossaryTerms { get; set; } = null!;
	[UsedImplicitly] public DbSet<SourceMaterial> SourceMaterials { get; set; } = null!;
	[UsedImplicitly] public DbSet<AssetModel> Assets { get; set; } = null!;

	protected override void OnModelCreating(ModelBuilder modelBuilder) {
		base.OnModelCreating(modelBuilder);

		modelBuilder.Entity<SeriesModel>(entity => {
			entity.Property(s => s.Id).ValueGeneratedOnAdd();

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

			entity.HasMany(s => s.GlossaryTerms)
				.WithOne(g => g.Series)
				.HasForeignKey(g => g.SeriesId)
				.OnDelete(DeleteBehavior.Cascade);

			entity.HasMany(s => s.SourceMaterials)
				.WithOne(sm => sm.Series)
				.HasForeignKey(sm => sm.SeriesId)
				.OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<VolumeModel>(entity => {
			entity.Property(v => v.Id).ValueGeneratedOnAdd();

			entity.Property(v => v.Title)
				.HasMaxLength(500)
				.IsRequired();

			entity.HasIndex(e => new { e.SeriesId, e.Order });
			entity.OwnsOne(s => s.Metadata, metaBuilder => {
				metaBuilder.ToJson();
			});
		});

		modelBuilder.Entity<ChapterModel>(entity => {
			entity.Property(c => c.Id).ValueGeneratedOnAdd();

			entity.Property(c => c.Title)
				.HasMaxLength(500)
				.IsRequired();

			entity.Property(c => c.Status)
				.HasConversion<int>();

			entity.HasIndex(e => new { e.VolumeId, e.Order });
			entity.HasIndex(c => c.Status); // Add index for Status queries

			entity.HasOne(c => c.ContentData)
				.WithOne(cc => cc.Chapter)
				.HasForeignKey<ChapterContentModel>(cc => cc.Id)
				.OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<ChapterContentModel>(entity => {
			entity.HasKey(cc => cc.Id);
			entity.Property(cc => cc.Id).ValueGeneratedOnAdd();

			entity.Property(cc => cc.Segments)
				.HasColumnType("jsonb")
				.HasConversion(
					v => JsonSerializer.Serialize(v, JsonConfiguration.JsonOptions),
					v => JsonSerializer.Deserialize<List<SegmentModel>>(v, JsonConfiguration.JsonOptions) ??
						new List<SegmentModel>()
					).Metadata.SetValueComparer(JsonConfiguration.ContentComparer);

			entity.Property(cc => cc.Footnotes)
				.HasColumnType("jsonb")
				.HasConversion(
					v => JsonSerializer.Serialize(v, JsonConfiguration.JsonOptions),
					v => JsonSerializer.Deserialize<List<FootnoteSegmentModel>>(
							v, JsonConfiguration.JsonOptions) ??
						new List<FootnoteSegmentModel>()
					).Metadata.SetValueComparer(JsonConfiguration.FootnoteComparer);
		});

		modelBuilder.Entity<GlossaryTerm>(entity => {
			entity.Property(g => g.Id).ValueGeneratedOnAdd();

			entity.Property(g => g.Term)
				.HasMaxLength(500)
				.IsRequired();

			entity.Property(g => g.Definition)
				.IsRequired();

			entity.Property(g => g.Type)
				.HasMaxLength(100);

			entity.HasIndex(g => g.Term);
			entity.HasIndex(g => g.SeriesId);
		});

		modelBuilder.Entity<SourceMaterial>(entity => {
			entity.Property(sm => sm.Id).ValueGeneratedOnAdd();

			entity.Property(sm => sm.Title)
				.HasMaxLength(500)
				.IsRequired();

			entity.Property(sm => sm.MarkdownContent)
				.HasColumnType("text")
				.IsRequired();

			entity.Property(sm => sm.SourceUrl)
				.HasMaxLength(2000);

			entity.HasIndex(sm => sm.SeriesId);
		});

		modelBuilder.Entity<AssetModel>(entity => {
			entity.Property(a => a.Id).ValueGeneratedOnAdd();

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
