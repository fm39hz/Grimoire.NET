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

		modelBuilder.Entity<SeriesModel>(static entity => {
			entity.Property(static s => s.Id).ValueGeneratedOnAdd();

			entity.Property(static s => s.Title)
				.HasMaxLength(500)
				.IsRequired();

			entity.Ignore(static s => s.Path);

			entity.Property(static s => s.DbPath)
				.HasColumnName("Path")
				.HasColumnType("ltree")
				.IsRequired();

			entity.Property(static s => s.Metadata)
				.HasColumnType("jsonb")
				.HasConversion(
					static v => JsonSerializer.Serialize(v, JsonConfiguration.JsonOptions),
					static v => JsonSerializer.Deserialize<SeriesMetadata>(v, JsonConfiguration.JsonOptions) ??
						new SeriesMetadata()
					)
				.Metadata.SetValueComparer(JsonConfiguration.MetadataComparer);
			entity.HasIndex(static s => s.Metadata).HasMethod("gin");
			entity.HasIndex(static s => s.Title).IsUnique();
			entity.HasIndex(static s => s.DbPath).HasMethod("gist");

			entity.HasMany(static s => s.GlossaryTerms)
				.WithOne(static g => g.Series)
				.HasForeignKey(static g => g.SeriesId)
				.OnDelete(DeleteBehavior.Cascade);

			entity.HasMany(static s => s.SourceMaterials)
				.WithOne(static sm => sm.Series)
				.HasForeignKey(static sm => sm.SeriesId)
				.OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<VolumeModel>(static entity => {
			entity.Property(static v => v.Id).ValueGeneratedOnAdd();

			entity.Property(static v => v.Title)
				.HasMaxLength(500)
				.IsRequired()
				.UseCollation("natural_sort");

			entity.Ignore(static v => v.Path);

			entity.Property(static v => v.DbPath)
				.HasColumnName("Path")
				.HasColumnType("ltree")
				.IsRequired();

			entity.HasIndex(static v => v.DbPath).HasMethod("gist");
			entity.HasIndex(static v => new { v.DbPath, v.Order }).IsUnique();
			entity.OwnsOne(static v => v.Metadata, static metaBuilder => metaBuilder.ToJson());
		});

		modelBuilder.Entity<ChapterModel>(static entity => {
			entity.Property(static c => c.Id).ValueGeneratedOnAdd();

			entity.Property(static c => c.Title)
				.HasMaxLength(500)
				.IsRequired()
				.UseCollation("natural_sort");

			entity.Property(static c => c.Status);

			entity.Ignore(static c => c.Path);

			entity.Property(static c => c.DbPath)
				.HasColumnName("Path")
				.HasColumnType("ltree")
				.IsRequired();

			entity.HasIndex(static c => c.DbPath).HasMethod("gist");
			entity.HasIndex(static c => new { c.DbPath, c.Order }).IsUnique();
			entity.HasIndex(static c => c.Status);
		});

		modelBuilder.Entity<SegmentModel>(static entity => {
			entity.HasKey(static s => s.Id);
			entity.Property(static s => s.Id).ValueGeneratedOnAdd();

			entity.Ignore(static s => s.Path);

			entity.Property(static s => s.DbPath)
				.HasColumnName("Path")
				.HasColumnType("ltree")
				.IsRequired();

			entity.Property(static s => s.Order).IsRequired();

			entity.HasIndex(static s => s.DbPath).HasMethod("gist");
			entity.HasIndex(static s => new { s.DbPath, s.Order }).IsUnique();

			entity.HasDiscriminator<string>("SegmentType")
				.HasValue<TextSegmentModel>("Text")
				.HasValue<ImageSegmentModel>("Image")
				.HasValue<DividerSegmentModel>("Divider")
				.HasValue<FootnoteSegmentModel>("Footnote");
		});

		modelBuilder.Entity<TextSegmentModel>(static entity => entity.Property(static t => t.Runs)
				.HasColumnType("jsonb")
				.HasConversion(
					static v => JsonSerializer.Serialize(v, JsonConfiguration.JsonOptions),
					static v => JsonSerializer.Deserialize<List<TextRun>>(v, JsonConfiguration.JsonOptions) ??
						new List<TextRun>()
				).Metadata.SetValueComparer(JsonConfiguration.TextRunComparer));

		modelBuilder.Entity<ImageSegmentModel>(static entity => {
			entity.Property(static i => i.AssetKey).HasMaxLength(500).IsRequired();
			entity.Property(static i => i.Caption).HasMaxLength(1000);
		});

		modelBuilder.Entity<FootnoteSegmentModel>(static entity => entity.Property(static f => f.Segments)
				.HasColumnType("jsonb")
				.HasConversion(
					static v => JsonSerializer.Serialize(v, JsonConfiguration.JsonOptions),
					static v => JsonSerializer.Deserialize<List<TextSegmentModel>>(v, JsonConfiguration.JsonOptions) ??
						new List<TextSegmentModel>()
				).Metadata.SetValueComparer(JsonConfiguration.FootnoteSegmentsComparer));

		modelBuilder.Entity<GlossaryTerm>(static entity => {
			entity.Property(static g => g.Id).ValueGeneratedOnAdd();

			entity.Property(static g => g.Term)
				.HasMaxLength(500)
				.IsRequired();

			entity.Property(static g => g.Definition)
				.IsRequired();

			entity.Property(static g => g.Type)
				.HasMaxLength(100);

			entity.HasIndex(static g => g.Term);
			entity.HasIndex(static g => g.SeriesId);
		});

		modelBuilder.Entity<SourceMaterial>(static entity => {
			entity.Property(static sm => sm.Id).ValueGeneratedOnAdd();

			entity.Property(static sm => sm.Title)
				.HasMaxLength(500)
				.IsRequired();

			entity.Property(static sm => sm.MarkdownContent)
				.HasColumnType("text")
				.IsRequired();

			entity.Property(static sm => sm.SourceUrl)
				.HasMaxLength(2000);

			entity.HasIndex(static sm => sm.SeriesId);
		});

		modelBuilder.Entity<AssetModel>(static entity => {
			entity.Property(static a => a.Id).ValueGeneratedOnAdd();

			entity.Property(static a => a.Path)
				.HasMaxLength(1000)
				.IsRequired();

			entity.Property(static a => a.FileHash)
				.HasMaxLength(64)
				.IsRequired();

			entity.Property(static a => a.RefType)
				.HasMaxLength(50)
				.IsRequired();

			entity.HasIndex(static a => a.SeriesId);
			entity.HasIndex(static a => a.OwnerNodeId);
			entity.HasIndex(static a => a.FileHash);
			entity.HasIndex(static a => new { a.SeriesId, a.FileHash });
			entity.HasIndex(static a => a.RefType);

		});

		modelBuilder.Entity<SeriesExportRecord>(static entity => {
			entity.Property(static e => e.Id).ValueGeneratedOnAdd();
			entity.Property(static e => e.Format).HasMaxLength(50).IsRequired();
			entity.HasIndex(static e => new { e.SeriesId, e.Format }).IsUnique();
		});
	}
}
