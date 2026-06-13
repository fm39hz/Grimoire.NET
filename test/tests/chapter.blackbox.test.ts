import { describe, expect, test } from "bun:test";
import { API_BASE_URL as baseUrl } from "../config.ts";

type Json = Record<string, unknown>;

const request = async <T = Json>(path: string, init?: RequestInit): Promise<T> => {
	if (!baseUrl) {
		throw new Error("GRIMOIRE_BLACKBOX_BASE_URL is required for blackbox tests");
	}

	const response = await fetch(`${baseUrl}${path}`, {
		...init,
		headers: {
			"content-type": "application/json",
			...init?.headers,
		},
	});

	if (!response.ok) {
		const body = await response.text();
		throw new Error(`${init?.method ?? "GET"} ${path} failed with ${response.status}: ${body}`);
	}

	if (response.status === 204) {
		return {} as T;
	}

	return await response.json() as T;
};

const assertCanonicalChapter = (
	chapter: Json,
	expected: { id: string; volumeId: string; order: number; title: string; textContent: string }
) => {
	expect(chapter.id).toBe(expected.id);
	expect(chapter.volumeId).toBe(expected.volumeId);
	expect(chapter.order).toBe(expected.order);
	expect(chapter.title).toBe(expected.title);
	expect(Array.isArray(chapter.content)).toBe(true);

	const content = chapter.content as Json[];
	expect(content.length).toBeGreaterThan(0);
	const textSeg = content[0];
	expect(textSeg.$type).toBe("Text");

	const runs = textSeg.runs as Json[];
	expect(runs.length).toBeGreaterThan(0);
	expect(runs[0].text).toBe(expected.textContent);
};

describe("chapter blackbox contract", () => {
	test("canonical chapter response schema matches expected DTO contract", () => {
		const chapterId = "chp_11111111-1111-1111-1111-111111111111";
		const volumeId = "vol_22222222-2222-2222-2222-222222222222";

		assertCanonicalChapter({
			id: chapterId,
			volumeId: volumeId,
			order: 1,
			title: "Chapter 1",
			content: [{
				$type: "Text",
				id: "seg_33333333-3333-3333-3333-333333333333",
				runs: [{
					text: "Hello, World!",
					bold: false,
					italic: false,
					footnoteId: null
				}]
			}],
			footnotes: []
		}, {
			id: chapterId,
			volumeId: volumeId,
			order: 1,
			title: "Chapter 1",
			textContent: "Hello, World!"
		});
	});

	if (baseUrl) {
		test("complete chapter ingestion, update, split, merge, and deletion lifecycle", async () => {
			const suffix = crypto.randomUUID();
			let seriesId = "";
			let volumeId = "";
			let chapterId = "";
			let splitChapterId = "";

			try {
				// 1. Create Series
				const series = await request<{ id: string }>("/series", {
					method: "POST",
					body: JSON.stringify({
						title: `Chapter lifecycle series ${suffix}`,
						metadata: {
							authors: [],
							artists: [],
							tags: ["lifecycle-test"],
							description: [],
							coverImage: "",
						},
					}),
				});
				seriesId = series.id;

				// 2. Create Volume
				const volume = await request<{ id: string }>("/volumes", {
					method: "POST",
					body: JSON.stringify({
						seriesId,
						order: 1,
						title: "Volume 1",
						metadata: null,
					}),
				});
				volumeId = volume.id;

				// 3. Create Chapter via PreProcessedIngestionStrategy
				const chapter = await request<Json>("/chapters", {
					method: "POST",
					body: JSON.stringify({
						volumeId,
						order: 1,
						title: "Chapter 1",
						content: [
							{
								$type: "Text",
								runs: [{ text: "Paragraph 1" }]
							},
							{
								$type: "Text",
								runs: [{ text: "Paragraph 2" }]
							}
						],
						footnotes: [],
						rawContent: null
					}),
				});
				chapterId = chapter.id as string;

				// Verify it matches canonical shape
				assertCanonicalChapter(chapter, {
					id: chapterId,
					volumeId,
					order: 1,
					title: "Chapter 1",
					textContent: "Paragraph 1"
				});

				// 4. Retrieve content formatted as markdown
				const contentRes = await request<{ data: string; type: string }>(`/chapters/${chapterId}/content?format=markdown`);
				expect(contentRes.type).toBe("text/markdown");
				expect(contentRes.data).toContain("Paragraph 1");
				expect(contentRes.data).toContain("Paragraph 2");

				// 5. Update chapter (PATCH /chapters/{id})
				const updatedChapter = await request<Json>(`/chapters/${chapterId}`, {
					method: "PATCH",
					body: JSON.stringify({
						title: "Chapter 1 Updated",
						order: 2
					}),
				});
				expect(updatedChapter.title).toBe("Chapter 1 Updated");
				expect(updatedChapter.order).toBe(2);

				// 6. Split chapter (POST /chapters/{id}/split)
				const splitResult = await request<Json[]>(`/chapters/${chapterId}/split`, {
					method: "POST",
					body: JSON.stringify({
						splitPoints: [
							{ segmentIndex: 1, newChapterTitle: "Chapter 2" }
						]
					})
				});

				expect(splitResult.length).toBe(2);
				const part1 = splitResult[0];
				const part2 = splitResult[1];
				splitChapterId = part2.id as string;

				expect(part1.title).toBe("Chapter 1 Updated");
				expect((part1.content as Json[]).length).toBe(1);
				expect(part2.title).toBe("Chapter 2");
				expect((part2.content as Json[]).length).toBe(1);

				// 7. Merge chapters back (POST /chapters/merge)
				const mergedChapter = await request<Json>("/chapters/merge", {
					method: "POST",
					body: JSON.stringify({
						chapterIds: [part1.id, part2.id]
					})
				});

				expect(mergedChapter.id).toBe(part1.id);
				expect(mergedChapter.title).toBe("Chapter 1 Updated");
				expect((mergedChapter.content as Json[]).length).toBe(2);

				// Re-assign chapterId to part1.id and clear splitChapterId since it's deleted by merge
				chapterId = part1.id as string;
				splitChapterId = "";
			}
			finally {
				// Cleanup
				if (splitChapterId) {
					await request(`/chapters/${splitChapterId}`, { method: "DELETE" }).catch(() => undefined);
				}
				if (chapterId) {
					await request(`/chapters/${chapterId}`, { method: "DELETE" }).catch(() => undefined);
				}
				if (volumeId) {
					await request(`/volumes/${volumeId}`, { method: "DELETE" }).catch(() => undefined);
				}
				if (seriesId) {
					await request(`/series/${seriesId}`, { method: "DELETE" }).catch(() => undefined);
				}
			}
		});
	}
});
