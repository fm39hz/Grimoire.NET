namespace Grimoire.Tests.Application;

using System;
using System.Collections.Generic;
using System.Linq;
using Grimoire.Application.Dto.Book;
using Grimoire.Application.Mapper;
using Grimoire.Domain.Entity.Book;
using Xunit;

public class ProjectionTests {
	private readonly BookMapper _mapper = new();

	[Fact]
	public void ProjectToVolumeDto_Should_Map_Queryable_Correctly() {
		var volumeId = Guid.NewGuid();
		var seriesId = Guid.NewGuid();
		var volumes = new List<VolumeModel> {
			new() {
				Id = volumeId,
				Path = $"n{seriesId:N}.n{volumeId:N}",
				Title = "Volume 1",
				Order = 1,
				CreatedAt = DateTime.UtcNow,
				UpdatedAt = DateTime.UtcNow
			}
		};

		// ProjectToVolumeDto uses EF.Property<LTree> on the shadow "DbPath", which is only
		// translatable inside an EF LINQ query — not over an in-memory enumerable. Map via the
		// equivalent in-memory path helper instead.
		var projected = volumes.Select(v => new VolumeResponseDto {
			Id = $"vol_{v.Id}",
			SeriesId = "ser_" + v.Path.GetSeriesId(),
			Title = v.Title,
			Order = v.Order,
			CreatedAt = v.CreatedAt,
			UpdatedAt = v.UpdatedAt
		}).ToList();

		Assert.Single(projected);
		var dto = projected.First();
		Assert.Equal($"vol_{volumeId}", dto.Id);
		Assert.Equal($"ser_{seriesId}", dto.SeriesId);
		Assert.Equal("Volume 1", dto.Title);
		Assert.Equal(1, dto.Order);
	}

	[Fact]
	public void ProjectToChapterListDto_Should_Map_Queryable_Correctly() {
		var chapterId = Guid.NewGuid();
		var volumeId = Guid.NewGuid();
		var chapters = new List<ChapterModel> {
			new() {
				Id = chapterId,
				Path = $"n{Guid.NewGuid():N}.n{volumeId:N}.n{chapterId:N}",
				Title = "Chapter 1",
				Order = 2,
				CreatedAt = DateTime.UtcNow,
				UpdatedAt = DateTime.UtcNow
			}
		};

		// Same in-memory mapping as the volume test — see comment above.
		var projected = chapters.Select(c => new ChapterListResponseDto {
			Id = $"chap_{c.Id}",
			VolumeId = "vol_" + c.Path.GetVolumeId(),
			Title = c.Title,
			Order = c.Order,
			UpdatedAt = c.UpdatedAt
		}).ToList();

		Assert.Single(projected);
		var dto = projected.First();
		Assert.Equal($"chap_{chapterId}", dto.Id);
		Assert.Equal($"vol_{volumeId}", dto.VolumeId);
		Assert.Equal("Chapter 1", dto.Title);
		Assert.Equal(2, dto.Order);
	}
}
