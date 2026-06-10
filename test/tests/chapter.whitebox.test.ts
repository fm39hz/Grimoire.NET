import { describe, expect, test } from "bun:test";
import path from "node:path";

const repoRoot = path.resolve(import.meta.dir, "../..");

const read = async (relativePath: string) =>
	await Bun.file(path.join(repoRoot, relativePath)).text();

describe("chapter & ingestion whitebox", () => {
	test("Ingestion strategy interfaces and implementations exist", async () => {
		const strategyInterface = await read("src/Grimoire.Application/Service/Strategy/IIngestionStrategy.cs");
		const preProcessedStrategy = await read("src/Grimoire.Application/Service/Strategy/PreProcessedIngestionStrategy.cs");
		const rawMarkdownStrategy = await read("src/Grimoire.Application/Service/Strategy/RawMarkdownIngestionStrategy.cs");
		const factory = await read("src/Grimoire.Application/Service/Strategy/IngestionStrategyFactory.cs");

		expect(strategyInterface).toContain("bool CanHandle(CreateChapterRequestDto dto)");
		expect(strategyInterface).toContain("Task<IngestionResult> ExecuteAsync");

		expect(preProcessedStrategy).toContain("class PreProcessedIngestionStrategy : IIngestionStrategy");
		expect(preProcessedStrategy).toContain("FootnoteRemapper.Remap");
		expect(preProcessedStrategy).toContain("ChapterStatus.Done");

		expect(rawMarkdownStrategy).toContain("class RawMarkdownIngestionStrategy");
		expect(rawMarkdownStrategy).toContain("HtmlTagRegex");
		expect(rawMarkdownStrategy).toContain("ChapterStatus.Draft");
		expect(rawMarkdownStrategy).toContain("IVolumeRepository volumeRepository");

		expect(factory).toContain("class IngestionStrategyFactory : IIngestionStrategyFactory");
		expect(factory).toContain("GetStrategy(CreateChapterRequestDto dto)");
	});

	test("ChapterService manages transactions, split, and merge operations", async () => {
		const service = await read("src/Grimoire.Application/Service/Implementation/ChapterService.cs");

		expect(service).toContain("MergeAsync(MergeChaptersRequestDto dto");
		expect(service).toContain("SplitAsync(Guid chapterId, SplitChapterRequestDto dto");
		expect(service).toContain("UpsertAsync(Guid volumeId, CreateChapterRequestDto dto");

		expect(service).toContain("unitOfWork.BeginTransactionAsync");
		expect(service).toContain("unitOfWork.CommitTransactionAsync");
		expect(service).toContain("unitOfWork.RollbackTransactionAsync");

		expect(service).toContain("bookTreeService.CreateNode");
		expect(service).toContain("bookTreeService.UpdateNode");
		expect(service).toContain("bookTreeService.DeleteSubtree");
	});

	test("Footnote remapper utility remaps temporary IDs to system GUIDs", async () => {
		const remapper = await read("src/Grimoire.Application/Common/FootnoteRemapper.cs");

		expect(remapper).toContain("class FootnoteRemapper");
		expect(remapper).toContain("Remap(");
		expect(remapper).toContain("ExtractReferencedIds(IEnumerable<SegmentModel> segments)");
		expect(remapper).toContain("Guid.CreateVersion7()");
	});
});
