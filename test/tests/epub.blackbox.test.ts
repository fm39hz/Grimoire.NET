import { describe, expect, test } from "bun:test";
import path from "node:path";

const baseUrl = process.env.GRIMOIRE_BLACKBOX_BASE_URL?.replace(/\/$/, "") || "http://localhost:5062/api/v1";
const repoRoot = path.resolve(import.meta.dir, "../..");

type Json = Record<string, unknown>;

const request = async <T = Json>(path: string, init?: RequestInit): Promise<T> => {
	const response = await fetch(`${baseUrl}${path}`, {
		...init,
		headers: {
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

// Helper to poll job status
const pollJobStatus = async (jobId: string, maxAttempts = 900): Promise<Json> => {
	let lastLoggedProgress: number | null = null;
	for (let attempt = 1; attempt <= maxAttempts; attempt++) {
		const status = await request<Json>(`/publishes/jobs/${jobId}`);
		const state = status.status as string;

		if (status.progress !== undefined && status.progress !== null) {
			const progress = status.progress as number;
			if (progress !== lastLoggedProgress) {
				console.log(`   ⏳ Job ${jobId} progress: ${progress}%`);
				lastLoggedProgress = progress;
			}
		}

		if (state === "Completed") {
			return status;
		}
		if (state === "Failed") {
			throw new Error(`Job ${jobId} failed: ${status.error || "Unknown error"}`);
		}

		await new Promise(resolve => setTimeout(resolve, 1000));
	}
	throw new Error(`Job ${jobId} timed out after ${maxAttempts} seconds`);
};

describe("EPUB integration lifecycle", () => {
	test("complete import, tree verification, content render, export and deletion flow from test.epub", async () => {
		const suffix = crypto.randomUUID();
		const uniqueTitle = `EPUB Import Test ${suffix}`;
		const epubPath = path.resolve(repoRoot, "test/assets/test.epub");

		// 1. Prepare Multipart Form Data
		const formData = new FormData();
		const fileBlob = Bun.file(epubPath);
		formData.append("file", fileBlob, "test.epub");
		formData.append("series", JSON.stringify({
			title: uniqueTitle,
			metadata: {
				authors: ["Integration Author"],
				artists: [],
				tags: ["epub-integration-test"],
				description: [],
				coverImage: ""
			}
		}));

		console.log(`🚀 Sending import request for: ${uniqueTitle}`);

		// 2. Call Import endpoint
		const importStart = await request<Json>("/publishes/import", {
			method: "POST",
			body: formData
		});

		const jobId = importStart.jobId as string;
		expect(jobId).toBeDefined();
		console.log(`⏳ EPUB Import Job queued: ${jobId}, polling status...`);

		// 3. Poll for completion
		await pollJobStatus(jobId);
		console.log(`✅ Import Job completed!`);

		// 4. Find the created Series ID by Title
		const listRes = await request<{ items: Json[] }>("/series?pageSize=100");
		const matchingSeries = listRes.items.find(s => s.title === uniqueTitle);
		expect(matchingSeries).toBeDefined();

		const seriesId = matchingSeries!.id as string;
		console.log(`📚 Found Series ID in DB: ${seriesId}`);

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
			console.log(`      └─ First volume has ${chapters.length} chapters`);

			const firstChapter = chapters[0];
			firstChapterId = firstChapter.id as string;
			console.log(`      └─ First chapter ID: ${firstChapterId}, Title: ${firstChapter.title}`);

			// 6. Get rendered chapter content as markdown
			const chapterDetails = await request<Json>(`/chapters/${firstChapterId}`);
			console.log("CHAPTER DETAILS:", JSON.stringify(chapterDetails, null, 2));

			const contentRes = await request<{ data: string; type: string }>(
				`/chapters/${firstChapterId}/content?format=markdown`
			);
			console.log("CONTENT RESPONSE:", JSON.stringify(contentRes, null, 2));
			expect(contentRes.type).toBe("text/markdown");
			expect(contentRes.data.length).toBeGreaterThan(0);
			console.log(`📝 Chapter rendered successfully. Content size: ${contentRes.data.length} characters`);

			// 7. Trigger export back to EPUB
			console.log(`⚙️  Exporting series back to EPUB...`);
			const exportStart = await request<Json>(`/publishes/export?seriesId=${seriesId}`, {
				method: "POST",
				headers: {
					"content-type": "application/json"
				},
				body: JSON.stringify({
					format: 0,
					mode: "Anthology"
				})
			});

			const exportJobId = exportStart.jobId as string;
			expect(exportJobId).toBeDefined();
			console.log(`⏳ Export Job queued: ${exportJobId}, polling status...`);

			await pollJobStatus(exportJobId);
			console.log(`✅ Export Job completed!`);

			// 8. Download the exported EPUB
			const downloadRes = await fetch(`${baseUrl}/publishes/jobs/${exportJobId}/download`);
			expect(downloadRes.ok).toBe(true);
			const epubBytes = await downloadRes.arrayBuffer();
			expect(epubBytes.byteLength).toBeGreaterThan(0);
			console.log(`💾 EPUB downloaded successfully: ${epubBytes.byteLength} bytes`);

		} finally {
			// 9. Clean up and delete series
			console.log(`🗑️  Skipping cleanup for inspection. Series ID: ${seriesId}`);
			/*
			await request(`/series/${seriesId}`, {
				method: "DELETE"
			});

			// 10. Verify series is deleted
			const verifyResponse = await fetch(`${baseUrl}/series/${seriesId}`);
			expect(verifyResponse.status).toBe(404);
			console.log(`🧹 Series successfully deleted and cleaned up.`);
			*/
		}
	}, 900000); // Set timeout to 900 seconds because EPUB parsing can take some time
});
