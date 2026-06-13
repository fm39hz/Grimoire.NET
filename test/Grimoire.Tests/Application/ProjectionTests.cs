namespace Grimoire.Tests.Application;

using System;
using System.Collections.Generic;
using System.Linq;
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
				SeriesId = seriesId,
				Title = "Volume 1",
				Order = 1,
				CreatedAt = DateTime.UtcNow,
				UpdatedAt = DateTime.UtcNow
			}
		}.AsQueryable();

		var projected = _mapper.ProjectToVolumeDto(volumes).ToList();

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
				VolumeId = volumeId,
				Title = "Chapter 1",
				Order = 2,
				CreatedAt = DateTime.UtcNow,
				UpdatedAt = DateTime.UtcNow
			}
		}.AsQueryable();

		var projected = _mapper.ProjectToChapterListDto(chapters).ToList();

		Assert.Single(projected);
		var dto = projected.First();
		Assert.Equal($"chap_{chapterId}", dto.Id);
		Assert.Equal($"vol_{volumeId}", dto.VolumeId);
		Assert.Equal("Chapter 1", dto.Title);
		Assert.Equal(2, dto.Order);
	}
}
