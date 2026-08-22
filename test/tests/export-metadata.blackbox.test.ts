/**
 * EPUB export fallback contract tests.
 *
 * Verifies the export pipeline emits ISBN into the OPF package metadata in
 * Single mode and omits it in Anthology, with stable package `uid` across
 * re-exports. Covers the defects fixed in the export fallback work:
 *  - ISBN was never surfaced into OEBPS/content.opf.
 *  - Package uid was a fresh random GUID per export.
 *  - ISBN was rendered unescaped (XSS).
 *
 * Requires a live API: GRIMOIRE_BLACKBOX_BASE_URL.
 * Owner: backend export pipeline.
 */
import { describe, expect, test } from "bun:test";
import path from "node:path";
import { unzipSync } from "fflate";
import { API_BASE_URL as baseUrl } from "../config.ts";

const repoRoot = path.resolve(import.meta.dir, "../..");

type Json = Record<string, unknown>;

const request = async <T = Json>(
	urlPath: string,
	init?: RequestInit,
): Promise<T> => {
	const response = await fetch(`${baseUrl}${urlPath}`, {
		...init,
	});
	if (!response.ok) {
		const body = await response.text();
		throw new Error(
			`${init?.method ?? "GET"} ${urlPath} failed with ${response.status}: ${body}`,
		);
	}
	if (response.status === 204) return {} as T;
	return (await response.json()) as T;
};

const pollJob = async (jobId: string, maxAttempts = 900): Promise<Json> => {
	for (let attempt = 1; attempt <= maxAttempts; attempt++) {
		const status = await request<Json>(`/publishes/jobs/${jobId}`);
		const state = status.status as string;
		if (state === "Completed") return status;
		if (state === "Failed")
			throw new Error(`Job ${jobId} failed: ${status.error ?? "unknown"}`);
		await new Promise((r) => setTimeout(r, 1000));
	}
	throw new Error(`Job ${jobId} timed out`);
};

/** Export a series and return the parsed OEBPS/content.opf text. */
const exportSeriesAndReadOpf = async (
	seriesId: string,
	mode: "Single" | "Anthology",
	targetVolumeIds: string[] = [],
): Promise<string> => {
	const exportStart = await request<Json>(
		`/publishes/series/${seriesId}/export`,
		{
			method: "POST",
			headers: { "content-type": "application/json" },
			body: JSON.stringify(
				mode === "Single"
					? { format: 0, mode: "Single", targetVolumeIds }
					: { format: 0, mode: "Anthology" },
			),
		},
	);
	const jobId = exportStart.jobId as string;
	expect(jobId).toBeDefined();
	await pollJob(jobId);

	const downloadRes = await fetch(`${baseUrl}/publishes/jobs/${jobId}/download`);
	expect(downloadRes.ok).toBe(true);
	const bytes = new Uint8Array(await downloadRes.arrayBuffer());
	expect(bytes.byteLength).toBeGreaterThan(0);

	const unzipped = unzipSync(bytes);
	const opfEntry = Object.keys(unzipped).find((p) =>
		p.endsWith("OEBPS/content.opf"),
	);
	if (!opfEntry) throw new Error("OEBPS/content.opf not found in exported epub");
	return new TextDecoder().decode(unzipped[opfEntry]);
};

/** Set a volume's ISBN (and optionally cover) via PATCH. */
const patchVolumeMetadata = async (
	volumeId: string,
	isbn: string | null,
): Promise<void> => {
	await request(`/volumes/${volumeId}`, {
		method: "PATCH",
		headers: { "content-type": "application/json" },
		body: JSON.stringify({
			metadata: { isbn, coverImage: null, publicationDate: null },
		}),
	});
};

describe("EPUB export fallback (ISBN/uid) — blackbox", () => {
	test("Single mode surfaces volume ISBN into OPF; Anthology omits it; uid is stable across re-exports", async () => {
		const epubPath = path.resolve(repoRoot, "test/assets/test.epub");
		const beforeList = await request<{ items: Json[] }>("/series?pageSize=100");
		const beforeIds = new Set(beforeList.items.map((s) => s.id as string));

		const formData = new FormData();
		formData.append("file", Bun.file(epubPath), "isbn-export-test.epub");
		const importStart = await request<Json>("/publishes/import", {
			method: "POST",
			body: formData,
		});
		const jobId = importStart.jobId as string;
		expect(jobId).toBeDefined();
		await pollJob(jobId);

		const afterList = await request<{ items: Json[] }>("/series?pageSize=100");
		const series = afterList.items.find((s) => !beforeIds.has(s.id as string));
		expect(series).toBeDefined();
		const seriesId = series!.id as string;

		// Pick the first volume to carry a real ISBN for the export.
		const tree = await request<Json>(`/series/${seriesId}/tree`);
		const seriesNode = (tree.root as Json).children as Json[];
		const volumes = (seriesNode[0]!.children as Json[]);
		expect(volumes.length).toBeGreaterThan(0);
		const volume = volumes[0]!;
		const volumeId = volume.id as string;

		const isbn = "9783161484100";
		await patchVolumeMetadata(volumeId, isbn);

		try {
			// Single mode: ISBN goes into OPF as urn:isbn:...
			const singleOpf = await exportSeriesAndReadOpf(
				seriesId,
				"Single",
				[volumeId],
			);
			expect(singleOpf).toContain(
				`<dc:identifier id="isbn-id">urn:isbn:${isbn}</dc:identifier>`,
			);
			// uid must be the ISBN-backed urn (stable), not a random GUID.
			expect(singleOpf).toContain("urn:isbn:");
			expect(singleOpf).not.toMatch(
				/<dc:identifier id="uid">[0-9a-f]{32}<\/dc:identifier>/,
			);

			// uid stable across re-exports of the same volume.
			const singleOpfRe = await exportSeriesAndReadOpf(
				seriesId,
				"Single",
				[volumeId],
			);
			const uidFirst = singleOpf.match(
				/<dc:identifier id="uid">([^<]+)<\/dc:identifier>/,
			)![1];
			const uidRe = singleOpfRe.match(
				/<dc:identifier id="uid">([^<]+)<\/dc:identifier>/,
			)![1];
			expect(uidRe).toBe(uidFirst);

			// Anthology mode: no single representative -> ISBN omitted from OPF.
			// uid falls back to a stable series-id-hex, still not a random GUID.
			const anthologyOpf = await exportSeriesAndReadOpf(seriesId, "Anthology");
			expect(anthologyOpf).not.toContain("urn:isbn:");
			const anthologyUid = anthologyOpf.match(
				/<dc:identifier id="uid">([^<]+)<\/dc:identifier>/,
			)![1];
			const anthologyUidRe = (
				await exportSeriesAndReadOpf(seriesId, "Anthology")
			).match(/<dc:identifier id="uid">([^<]+)<\/dc:identifier>/)![1];
			expect(anthologyUidRe).toBe(anthologyUid);

			// Title still rendered (sanity — content.opf is well-formed).
			expect(anthologyOpf).toContain("<dc:title>");
		} finally {
			// Leave series for other tests; cleanup intentionally disabled like epub.blackbox.
		}
	}, 900000);

	test("ISBN with HTML characters is escaped in the OPF (XSS hardening)", async () => {
		const epubPath = path.resolve(repoRoot, "test/assets/test.epub");
		const beforeList = await request<{ items: Json[] }>("/series?pageSize=100");
		const beforeIds = new Set(beforeList.items.map((s) => s.id as string));

		const formData = new FormData();
		formData.append("file", Bun.file(epubPath), "isbn-xss-test.epub");
		const jobId = (await request<Json>("/publishes/import", {
			method: "POST",
			body: formData,
		})).jobId as string;
		await pollJob(jobId);

		const afterList = await request<{ items: Json[] }>("/series?pageSize=100");
		const series = afterList.items.find((s) => !beforeIds.has(s.id as string));
		const seriesId = series!.id as string;
		const tree = await request<Json>(`/series/${seriesId}/tree`);
		const seriesNode = (tree.root as Json).children as Json[];
		const volumes = (seriesNode[0]!.children as Json[]);
		const volumeId = volumes[0]!.id as string;

		// An ISBN carrying markup must not inject raw HTML into the OPF.
		const poisonIsbn = `<script>alert(1)</script>`;
		await patchVolumeMetadata(volumeId, poisonIsbn);

		try {
			const opf = await exportSeriesAndReadOpf(seriesId, "Single", [volumeId]);
			expect(opf).not.toContain(`<script>alert(1)</script>`);
			expect(opf).toContain("&lt;script&gt;");
		} finally {
			// cleanup intentionally disabled
		}
	}, 900000);
});