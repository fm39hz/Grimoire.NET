namespace Grimoire.Infrastructure.Persistence.Database;

using System.Text.Json;
using Configuration;
using Domain.Entity.Book;
using Domain.Entity.Book.Metadata;
using Domain.Entity.Book.Segment;
using Grimoire.Domain.Entity;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options) {
	[UsedImplicitly] public DbSet<SeriesModel> Series { get; set; } = null!;
	[UsedImplicitly] public DbSet<VolumeModel> Volumes { get; set; } = null!;
	[UsedImplicitly] public DbSet<ChapterModel> Chapters { get; set; } = null!;
	[UsedImplicitly] public DbSet<SegmentModel> Segments { get; set; } = null!;
	[UsedImplicitly] public DbSet<GlossaryTerm> GlossaryTerms { get; set; } = null!;
	[UsedImplicitly] public DbSet<SourceMaterial> SourceMaterials { get; set; } = null!;
	[UsedImplicitly] public DbSet<AssetModel> Assets { get; set; } = null!;
	[UsedImplicitly] public DbSet<SeriesExportRecord> SeriesExportRecords { get; set; } = null!;

	protected override void OnModelCreating(ModelBuilder modelBuilder) {
		base.OnModelCreating(modelBuilder);

		modelBuilder.HasPostgresEnum<BookNodeType>();
		modelBuilder.HasPostgresEnum<ChapterStatus>();
		modelBuilder.HasCollation("natural_sort", locale: "en-US-u-kn-true", provider: "icu", deterministic: false);

		modelBuilder.HasPostgresExtension("ltree");

		modelBuilder.Entity<SeriesModel>(entity => {
			entity.Property(s => s.Id).ValueGeneratedOnAdd();

			entity.Property(s => s.Title)
				.HasMaxLength(500)
				.IsRequired();

			entity.Ignore(s => s.Path);

			entity.Property(s => s.DbPath)
				.HasColumnName("Path")
				.HasColumnType("ltree")
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
			entity.HasIndex(s => s.DbPath).HasMethod("gist");

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
				.IsRequired()
				.UseCollation("natural_sort");

			entity.Ignore(v => v.Path);

			entity.Property(v => v.DbPath)
				.HasColumnName("Path")
				.HasColumnType("ltree")
				.IsRequired();

			entity.HasIndex(v => v.DbPath).HasMethod("gist");
			entity.HasIndex(v => new { v.DbPath, v.Order }).IsUnique();
			entity.OwnsOne(v => v.Metadata, metaBuilder => metaBuilder.ToJson());
		});

		modelBuilder.Entity<ChapterModel>(entity => {
			entity.Property(c => c.Id).ValueGeneratedOnAdd();

			entity.Property(c => c.Title)
				.HasMaxLength(500)
				.IsRequired()
				.UseCollation("natural_sort");

			entity.Property(c => c.Status);

			entity.Ignore(c => c.Path);

			entity.Property(c => c.DbPath)
				.HasColumnName("Path")
				.HasColumnType("ltree")
				.IsRequired();

			entity.HasIndex(c => c.DbPath).HasMethod("gist");
			entity.HasIndex(c => new { c.DbPath, c.Order }).IsUnique();
			entity.HasIndex(c => c.Status);
		});

		modelBuilder.Entity<SegmentModel>(entity => {
			entity.HasKey(s => s.Id);
			entity.Property(s => s.Id).ValueGeneratedOnAdd();

			entity.Ignore(s => s.Path);

			entity.Property(s => s.DbPath)
				.HasColumnName("Path")
				.HasColumnType("ltree")
				.IsRequired();

			entity.Property(s => s.Order).IsRequired();

			entity.HasIndex(s => s.DbPath).HasMethod("gist");
			entity.HasIndex(s => new { s.DbPath, s.Order }).IsUnique();

			entity.HasDiscriminator<string>("SegmentType")
				.HasValue<TextSegmentModel>("Text")
				.HasValue<ImageSegmentModel>("Image")
				.HasValue<DividerSegmentModel>("Divider")
				.HasValue<FootnoteSegmentModel>("Footnote");
		});

		modelBuilder.Entity<TextSegmentModel>(entity => {
			entity.Property(t => t.Runs)
				.HasColumnType("jsonb")
				.HasConversion(
					v => JsonSerializer.Serialize(v, JsonConfiguration.JsonOptions),
					v => JsonSerializer.Deserialize<List<TextRun>>(v, JsonConfiguration.JsonOptions) ??
						new List<TextRun>()
				).Metadata.SetValueComparer(JsonConfiguration.TextRunComparer);
		});

		modelBuilder.Entity<ImageSegmentModel>(entity => {
			entity.Property(i => i.AssetKey).HasMaxLength(500).IsRequired();
			entity.Property(i => i.Caption).HasMaxLength(1000);
		});

		modelBuilder.Entity<FootnoteSegmentModel>(entity => {
			entity.Property(f => f.Segments)
				.HasColumnType("jsonb")
				.HasConversion(
					v => JsonSerializer.Serialize(v, JsonConfiguration.JsonOptions),
					v => JsonSerializer.Deserialize<List<TextSegmentModel>>(v, JsonConfiguration.JsonOptions) ??
						new List<TextSegmentModel>()
				).Metadata.SetValueComparer(JsonConfiguration.FootnoteSegmentsComparer);
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
			entity.HasIndex(a => a.OwnerNodeId);
			entity.HasIndex(a => a.FileHash);
			entity.HasIndex(a => new { a.SeriesId, a.FileHash });
			entity.HasIndex(a => a.RefType);

		});

		modelBuilder.Entity<SeriesExportRecord>(entity => {
			entity.Property(e => e.Id).ValueGeneratedOnAdd();
			entity.Property(e => e.Format).HasMaxLength(50).IsRequired();
			entity.HasIndex(e => new { e.SeriesId, e.Format }).IsUnique();
		});
	}
}
