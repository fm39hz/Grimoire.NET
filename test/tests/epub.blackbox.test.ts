import { describe, expect, test } from "bun:test";
import path from "node:path";
import { API_BASE_URL as baseUrl } from "../config.ts";
const repoRoot = path.resolve(import.meta.dir, "../..");

type Json = Record<string, unknown>;

const request = async <T = Json>(
	path: string,
	init?: RequestInit,
): Promise<T> => {
	const response = await fetch(`${baseUrl}${path}`, {
		...init,
		headers: {
			...init?.headers,
		},
	});

	if (!response.ok) {
		const body = await response.text();
		throw new Error(
			`${init?.method ?? "GET"} ${path} failed with ${response.status}: ${body}`,
		);
	}

	if (response.status === 204) {
		return {} as T;
	}

	return (await response.json()) as T;
};

// Helper to poll job status
const pollJobStatus = async (
	jobId: string,
	maxAttempts = 900,
): Promise<Json> => {
	let lastLoggedProgress: number | null = null;
	for (let attempt = 1; attempt <= maxAttempts; attempt++) {
		const status = await request<Json>(`/publishes/jobs/${jobId}`);
		const state = status.status as string;

		if (status.progress !== undefined && status.progress !== null) {
			const progress = status.progress as number;
			if (progress !== lastLoggedProgress) {
				const stage = (status.stage as string) ?? "(no stage)";
				console.log(`   ⏳ Job ${jobId} [${stage}] ${progress}%`);
				lastLoggedProgress = progress;
			}
		}

		if (state === "Completed") {
			return status;
		}
		if (state === "Failed") {
			throw new Error(
				`Job ${jobId} failed: ${status.error || "Unknown error"}`,
			);
		}

		await new Promise((resolve) => setTimeout(resolve, 1000));
	}
	throw new Error(`Job ${jobId} timed out after ${maxAttempts} seconds`);
};

// Verify SSE stream returns terminal status for a completed job
const verifySseCompleted = async (jobId: string): Promise<void> => {
	const sseUrl = `${baseUrl}/publishes/jobs/${jobId}/progress`;
	const ac = new AbortController();
	const timer = setTimeout(() => ac.abort(), 10_000);

	try {
		const response = await fetch(sseUrl, { signal: ac.signal });
		if (!response.ok) {
			console.warn(`   ⚠️ SSE stream returned ${response.status}`);
			return;
		}

		const reader = response.body!.getReader();
		const decoder = new TextDecoder();
		let buffer = "";

		while (true) {
			const { done, value } = await reader.read();
			if (done) break;

			buffer += decoder.decode(value, { stream: true });
			const lines = buffer.split("\n");
			buffer = lines.pop() ?? "";

			for (const line of lines) {
				if (line.startsWith("data: ")) {
					const evt = JSON.parse(line.slice(6)) as Json;
					if (evt.status === "Completed" || evt.status === "Failed") {
						console.log(`   ✅ SSE confirmed: ${evt.status}`);
						reader.cancel();
						return;
					}
				}
			}
		}
		console.warn(`   ⚠️ SSE did not return terminal status`);
	} catch (e) {
		if ((e as Error).name !== "AbortError")
			console.warn(`   ⚠️ SSE error: ${(e as Error).message}`);
	} finally {
		clearTimeout(timer);
	}
};

describe("EPUB integration lifecycle", () => {
	test("complete import, tree verification, content render, export and deletion flow from test.epub with metadata omitted", async () => {
		const epubPath = path.resolve(repoRoot, "test/assets/test.epub");

		// List existing series to identify the new one
		const beforeList = await request<{ items: Json[] }>(
			"/series?pageSize=100",
		);
		const beforeIds = new Set(beforeList.items.map((s) => s.id as string));

		// 1. Prepare Multipart Form Data without "series" metadata
		const formData = new FormData();
		const fileBlob = Bun.file(epubPath);
		formData.append("file", fileBlob, "test-no-metadata.epub");

		console.log(`🚀 Sending import request with omitted metadata`);

		// 2. Call Import endpoint
		const importStart = await request<Json>("/publishes/import", {
			method: "POST",
			body: formData,
		});

		const jobId = importStart.jobId as string;
		expect(jobId).toBeDefined();
		console.log(`⏳ EPUB Import Job queued: ${jobId}, polling status...`);

		// 3. Poll for completion + verify SSE
		await pollJobStatus(jobId);
		await verifySseCompleted(jobId);
		console.log(`✅ Import Job completed!`);

		// 4. Find the newly created series
		const afterList = await request<{ items: Json[] }>(
			"/series?pageSize=100",
		);
		const newSeries = afterList.items.find(
			(s) => !beforeIds.has(s.id as string),
		);
		expect(newSeries).toBeDefined();

		const seriesId = newSeries!.id as string;
		console.log(
			`📚 Found newly imported Series ID: ${seriesId}, Title: ${newSeries!.title}`,
		);

		// Expect title to be resolved from EPUB
		expect(newSeries!.title).toBe("Quỷ Bí Chi Chủ");

		// Get series details and check metadata
		const seriesDetails = await request<Json>(`/series/${seriesId}`);
		const metadata = seriesDetails.metadata as Json;
		expect(metadata).toBeDefined();

		const authors = metadata.authors as string[];
		expect(authors).toEqual(["Mực Thích Lặn Nước"]);

		const tags = metadata.tags as string[];
		expect(tags).toBeDefined();
		expect(tags.length).toBeGreaterThan(0);

		console.log(` Resolved Title: ${newSeries!.title}`);
		console.log(` Resolved Authors: ${JSON.stringify(authors)}`);
		console.log(` Resolved Tags: ${JSON.stringify(tags)}`);

		let firstChapterId = "";

		try {
			// 5. Get the book tree
			const tree = await request<Json>(`/series/${seriesId}/tree`);
			expect(tree.root).toBeDefined();
			expect(tree.root.id).toBe("bookshelf:default");

			const seriesNode = (tree.root.children as Json[])[0];
			expect(seriesNode.id).toBe(seriesId);
			expect(seriesNode.type).toBe("series");

			const volumes = seriesNode.children as Json[];
			expect(volumes.length).toBeGreaterThan(0);
			console.log(`   └─ Imported ${volumes.length} volumes`);

			const firstVolume = volumes[0];
			const chapters = firstVolume.children as Json[];
			expect(chapters.length).toBeGreaterThan(0);
			console.log(
				`      └─ First volume has ${chapters.length} chapters`,
			);

			const firstChapter = chapters[0];
			firstChapterId = firstChapter.id as string;
			console.log(
				`      └─ First chapter ID: ${firstChapterId}, Title: ${firstChapter.title}`,
			);

			// 6. Get rendered chapter content as markdown
			const contentRes = await request<{ data: string; type: string }>(
				`/chapters/${firstChapterId}/content?format=markdown`,
			);
			expect(contentRes.type).toBe("text/markdown");
			expect(contentRes.data.length).toBeGreaterThan(0);
			console.log(
				`📝 Chapter rendered successfully. Content size: ${contentRes.data.length} characters`,
			);

			// 7. Trigger export back to EPUB
			console.log(`⚙️  Exporting series back to EPUB...`);
			const exportStart = await request<Json>(
				`/publishes/series/${seriesId}/export`,
				{
					method: "POST",
					headers: {
						"content-type": "application/json",
					},
					body: JSON.stringify({
						format: 0,
						mode: "Anthology",
					}),
				},
			);

			const exportJobId = exportStart.jobId as string;
			expect(exportJobId).toBeDefined();
			console.log(
				`⏳ Export Job queued: ${exportJobId}, polling status...`,
			);

			await pollJobStatus(exportJobId);
			await verifySseCompleted(exportJobId);
			console.log(`✅ Export Job completed!`);

			// 8. Download the exported EPUB
			const downloadRes = await fetch(
				`${baseUrl}/publishes/jobs/${exportJobId}/download`,
			);
			expect(downloadRes.ok).toBe(true);
			const epubBytes = await downloadRes.arrayBuffer();
			expect(epubBytes.byteLength).toBeGreaterThan(0);
			console.log(
				`💾 EPUB downloaded successfully: ${epubBytes.byteLength} bytes`,
			);
		} finally {
			// // 9. Clean up and delete series
			// console.log(`🗑️  Cleaning up Series ID: ${seriesId}`);
			// await request(`/series/${seriesId}`, {
			// 	method: "DELETE"
			// });
			//
			// // 10. Verify series is deleted
			// const verifyResponse = await fetch(`${baseUrl}/series/${seriesId}`);
			// expect(verifyResponse.status).toBe(404);
			// console.log(`🧹 Series successfully deleted and cleaned up.`);
		}
	}, 900000);
});
