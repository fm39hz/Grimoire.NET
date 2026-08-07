import { describe, expect, test } from "bun:test";
import path from "node:path";

const repoRoot = path.resolve(import.meta.dir, "../..");

const read = async (relativePath: string) =>
	await Bun.file(path.join(repoRoot, relativePath)).text();

describe("unified book tree whitebox", () => {
	test("Series/Volume/Chapter/Segment are the persisted hierarchy models and BookShelf stays logical", async () => {
		const nodeType = await read("src/Grimoire.Domain/Entity/Book/BookNodeType.cs");
		const seriesModel = await read("src/Grimoire.Domain/Entity/Book/SeriesModel.cs");
		const volumeModel = await read("src/Grimoire.Domain/Entity/Book/VolumeModel.cs");
		const chapterModel = await read("src/Grimoire.Domain/Entity/Book/ChapterModel.cs");
		const segmentModel = await read("src/Grimoire.Domain/Entity/Book/SegmentModel.cs");

		expect(nodeType).toContain("Series");
		expect(nodeType).toContain("Volume");
		expect(nodeType).toContain("Chapter");
		expect(nodeType).not.toContain("BookShelf");

		// The four node types are persisted entities with an ltree DbPath.
		expect(seriesModel).toContain("LTree DbPath");
		expect(volumeModel).toContain("LTree DbPath");
		expect(chapterModel).toContain("LTree DbPath");
		expect(segmentModel).toContain("LTree DbPath");
	});

	test("EF mapping of the book tree tables", async () => {
		const dbContext = await read("src/Grimoire.Infrastructure/Persistence/Database/ApplicationDbContext.cs");

		expect(dbContext).toContain("DbSet<SeriesModel> Series");
		expect(dbContext).toContain("DbSet<VolumeModel> Volumes");
		expect(dbContext).toContain("DbSet<ChapterModel> Chapters");
		expect(dbContext).toContain("DbSet<SegmentModel> Segments");
		expect(dbContext).toContain("new { s.DbPath, s.Order }).IsUnique()");
		expect(dbContext).toContain("HasDiscriminator<string>(\"SegmentType\")");
		expect(dbContext).toContain("HasColumnType(\"ltree\")");
	});

	test("BookTreeService owns tree operations and service facades delegate to it", async () => {
		const treeService = await read("src/Grimoire.Application/Service/Implementation/BookTreeService.cs");
		const seriesService = await read("src/Grimoire.Application/Service/Implementation/SeriesService.cs");
		const volumeService = await read("src/Grimoire.Application/Service/Implementation/VolumeService.cs");
		const chapterService = await read("src/Grimoire.Application/Service/Implementation/ChapterService.cs");

		expect(treeService).toContain("GetTree");
		expect(treeService).toContain("CreateSeries");
		expect(treeService).toContain("CreateVolume");
		expect(treeService).toContain("MoveNode");
		expect(treeService).toContain("DeleteSubtree");
		expect(treeService).toContain("ExecuteInTransaction");
		expect(seriesService).toContain("seriesNodeService.CreateSeries");
		expect(seriesService).toContain("seriesNodeService.GetOrCreateSeries");
		expect(volumeService).toContain("volumeNodeService.CreateVolume");
		expect(volumeService).toContain("chapterNodeService.FindChapters");
		expect(chapterService).toContain("bookTreeService.MoveNode");
		expect(chapterService).toContain("bookTreeService.DeleteSubtree");
	});

	test("export and freshness use the tree source of truth", async () => {
		const context = await read("src/Grimoire.Application/Export/BookExportContext.cs");
		const resolver = await read("src/Grimoire.Application/Export/VolumeResolver.cs");
		const freshness = await read("src/Grimoire.Infrastructure/Persistence/Repository/SeriesExportRecordRepository.cs");

		expect(context).toContain("BookTreeDto Tree");
		expect(resolver).toContain("bookTreeService.FindVolumes");
		expect(freshness.toLowerCase()).toContain("context.segments");
		expect(freshness.toLowerCase()).not.toContain("context.booknodes");
	});
});
