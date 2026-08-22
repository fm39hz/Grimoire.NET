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

const contentRequest = async <T = Json>(path: string): Promise<{ data: string; type: string }> =>
	request<T>(path).then((r) => r as unknown as { data: string; type: string });

const countTypes = (segments: Json[]): Record<string, number> => {
	const counts: Record<string, number> = {};
	for (const s of segments) {
		const type = s.$type as string;
		counts[type] = (counts[type] ?? 0) + 1;
	}
	return counts;
};

const FIXTURE = [
	"Para one.",
	"",
	"**Bold** and *italic* with ==highlight== and ~~strike~~ and `code`.",
	"",
	"---",
	"",
	"| A | B |",
	"| --- | --- |",
	"| 1 | 2 |",
].join("\n");

describe("content roundtrip contract", () => {
	test("parser maps markdown onto typed segments and is stable on re-import", async () => {
		if (!baseUrl) {
			return;
		}
		let seriesId = "";
		let volumeId = "";
		let chapterId = "";
		let secondChapterId = "";

		try {
			// 1. Create series + volume
			const series = await request<{ id: string }>("/series", {
				method: "POST",
				body: JSON.stringify({
					title: `roundtrip-${crypto.randomUUID()}`,
					metadata: {
						authors: [],
						artists: [],
						tags: ["roundtrip-test"],
						description: [],
					},
				}),
			});
			seriesId = series.id;

			const tree = await request<Json>(`/series/${seriesId}/tree`);
			const root = tree.root as Json;
			const shelfChildren = (root.children ?? []) as Json[];
			expect(shelfChildren.length).toBeGreaterThan(0);

			const volume = await request<{ id: string }>("/volumes", {
				method: "POST",
				body: JSON.stringify({ seriesId, title: "Volume 1", order: 1 }),
			});
			volumeId = volume.id;

			// 2. Push fixture as rawContent (Obsidian push path)
			const chapter = await request<Json>("/chapters", {
				method: "POST",
				body: JSON.stringify({
					volumeId,
					order: 1,
					title: "Roundtrip",
					rawContent: FIXTURE,
				}),
			});
			chapterId = chapter.id as string;

			// 3. Typed segment inventory
			const content = (chapter.content ?? []) as Json[];
			const counts = countTypes(content);

			expect(counts["Text"]).toBe(2);
			expect(counts["Divider"]).toBe(1);
			expect(counts["Table"]).toBe(1);

			// Bold run flag survives the wire
			const boldPara = content.find((s) =>
				((s.runs ?? []) as Json[]).some((r) => r.isBold === true)
			) as Json | undefined;
			expect(boldPara).toBeDefined();

			const table = content.find((s) => s.$type === "Table") as Json;
			expect((table.header as Json[]).length).toBe(2);
			expect((table.rows as Json[][]).length).toBe(1);
			expect((table.rows[0] as Json[]).length).toBe(2);

			// 4. Pull rendered markdown contains the constructs
			const rendered = await contentRequest(`/chapters/${chapterId}/content?format=markdown`);
			expect(rendered.type).toBe("text/markdown");
			expect(rendered.data).toContain("| A | B |");
			expect(rendered.data).toContain("---");

			// 5. Re-push the pulled markdown → inventory must be identical (fixed point)
			const secondChapter = await request<Json>("/chapters", {
				method: "POST",
				body: JSON.stringify({
					volumeId,
					order: 2,
					title: "Roundtrip 2",
					rawContent: rendered.data,
				}),
			});
			secondChapterId = secondChapter.id as string;

			const secondContent = (secondChapter.content ?? []) as Json[];
			expect(countTypes(secondContent)).toEqual(counts);

			const secondRendered = await contentRequest(`/chapters/${secondChapterId}/content?format=markdown`);
			expect(secondRendered.data).toContain("| A | B |");
			expect(secondRendered.data).toContain("---");
		} finally {
			if (chapterId) await request(`/chapters/${chapterId}`, { method: "DELETE" }).catch(() => undefined);
			if (secondChapterId) await request(`/chapters/${secondChapterId}`, { method: "DELETE" }).catch(() => undefined);
			if (volumeId) await request(`/volumes/${volumeId}`, { method: "DELETE" }).catch(() => undefined);
			if (seriesId) await request(`/series/${seriesId}`, { method: "DELETE" }).catch(() => undefined);
		}
	});
});
