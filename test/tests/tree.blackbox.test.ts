import { describe, expect, test } from "bun:test";

const baseUrl = process.env.GRIMOIRE_BLACKBOX_BASE_URL?.replace(/\/$/, "");

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

describe("unified book tree blackbox", () => {
	test("canonical tree response contract exposes logical shelf root and typed parent links", () => {
		const seriesId = "ser_11111111-1111-1111-1111-111111111111";
		const volumeId = "vol_22222222-2222-2222-2222-222222222222";
		const chapterId = "chp_33333333-3333-3333-3333-333333333333";

		assertCanonicalTree({
			root: {
				id: "bookshelf:default",
				type: "bookShelf",
				title: "Book Shelf",
				children: [{
					id: seriesId,
					type: "series",
					title: "Series",
					parentId: "bookshelf:default",
					children: [{
						id: volumeId,
						type: "volume",
						title: "Volume 1",
						order: 1,
						parentId: seriesId,
						children: [{
							id: chapterId,
							type: "chapter",
							title: "Chapter 1",
							order: 1,
							parentId: volumeId,
							children: [],
						}],
					}],
				}],
			},
		}, { seriesId, volumeId, chapterId });
	});

	if (baseUrl) {
		test("legacy create endpoints produce canonical series tree", async () => {
			const suffix = crypto.randomUUID();
			let seriesId = "";
			let volumeId = "";
			let chapterId = "";

			try {
				const series = await request<{ id: string }>("/series", {
					method: "POST",
					body: JSON.stringify({
						title: `Tree Blackbox ${suffix}`,
						metadata: {
							authors: [],
							artists: [],
							tags: ["tree-test"],
							description: [],
							coverImage: "",
						},
					}),
				});
				seriesId = series.id;

				const volume = await request<{ id: string }>("/volume", {
					method: "POST",
					body: JSON.stringify({
						seriesId,
						order: 1,
						title: "Volume 1",
						metadata: null,
					}),
				});
				volumeId = volume.id;

				const chapter = await request<{ id: string }>("/chapter", {
					method: "POST",
					body: JSON.stringify({
						volumeId,
						order: 1,
						title: "Chapter 1",
						content: [],
						footnotes: [],
						rawContent: null,
					}),
				});
				chapterId = chapter.id;

				const tree = await request<{
					root: {
						id: string;
						type: string;
						children: Array<{
							id: string;
							type: string;
							parentId: string;
							children: Array<{
								id: string;
								type: string;
								parentId: string;
								children: Array<{ id: string; type: string; parentId: string }>;
							}>;
						}>;
					};
				}>(`/series/${seriesId}/tree`);

				assertCanonicalTree(tree, { seriesId, volumeId, chapterId });
			}
			finally {
				if (chapterId) {
					await request(`/chapter/${chapterId}`, { method: "DELETE" }).catch(() => undefined);
				}
				if (volumeId) {
					await request(`/volume/${volumeId}`, { method: "DELETE" }).catch(() => undefined);
				}
				if (seriesId) {
					await request(`/series/${seriesId}`, { method: "DELETE" }).catch(() => undefined);
				}
			}
		});
	}
});

const assertCanonicalTree = (
	tree: {
		root: {
			id: string;
			type: string;
			title?: string;
			children: Array<{
				id: string;
				type: string;
				parentId: string;
				children: Array<{
					id: string;
					type: string;
					parentId: string;
					children: Array<{ id: string; type: string; parentId: string; children?: unknown[] }>;
				}>;
			}>;
		};
	},
	ids: { seriesId: string; volumeId: string; chapterId: string },
) => {
	expect(tree.root.id).toBe("bookshelf:default");
	expect(tree.root.type).toBe("bookShelf");
	const seriesNode = tree.root.children[0];
	expect(seriesNode.id).toBe(ids.seriesId);
	expect(seriesNode.type).toBe("series");
	expect(seriesNode.parentId).toBe("bookshelf:default");
	const volumeNode = seriesNode.children[0];
	expect(volumeNode.id).toBe(ids.volumeId);
	expect(volumeNode.type).toBe("volume");
	expect(volumeNode.parentId).toBe(ids.seriesId);
	const chapterNode = volumeNode.children[0];
	expect(chapterNode.id).toBe(ids.chapterId);
	expect(chapterNode.type).toBe("chapter");
	expect(chapterNode.parentId).toBe(ids.volumeId);
	expect(chapterNode.children ?? []).toEqual([]);
};
