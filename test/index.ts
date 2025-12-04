import axios from "axios";
import fs from "fs-extra";
import path from "path";
import FormData from "form-data";
import { fileURLToPath } from "url";

const API_URL = "http://localhost:5062/api/v1";
const DIST_DIR = "./dist_grimoire";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const api = axios.create({
	baseURL: API_URL,
	maxContentLength: Infinity,
	maxBodyLength: Infinity,
});

api.interceptors.response.use(
	(res) => res,
	(err) => {
		if (err.response) {
			console.error(
				`❌ [${err.response.status}] ${err.config.method.toUpperCase()} ${err.config.url}`,
			);
			// Log ngắn gọn hơn để đỡ rối mắt
			if (err.response.status === 400)
				console.error(
					"   Data Error:",
					JSON.stringify(err.response.data),
				);
		} else {
			console.error(`❌ Network Error: ${err.message}`);
		}
		return Promise.reject(err);
	},
);

async function uploadAsset(localPath, contextId) {
	// Kiểm tra file tồn tại
	if (!fs.existsSync(localPath)) {
		// Thử fix lỗi đường dẫn encode URL (ví dụ %20 -> space)
		const decodedPath = decodeURIComponent(localPath);
		if (fs.existsSync(decodedPath)) {
			localPath = decodedPath;
		} else {
			console.warn(`   ⚠️ Missing: ${path.basename(localPath)}`);
			return null;
		}
	}

	try {
		const form = new FormData();
		form.append("file", fs.createReadStream(localPath));
		const res = await api.post(`/file/upload/${contextId}`, form, {
			headers: form.getHeaders(),
			params: { file: "Image" },
		});
		return res.data.path;
	} catch (e) {
		return null;
	}
}

async function processSegments(segments, assetsRoot, seriesId) {
	if (!segments || !Array.isArray(segments)) return segments;

	for (const seg of segments) {
		if (seg.$type === "Image" && seg.assetKey) {
			// Logic ghép đường dẫn tương đối
			const fullLocalPath = path.resolve(assetsRoot, seg.assetKey);

			const serverPath = await uploadAsset(fullLocalPath, seriesId);
			if (serverPath) {
				seg.assetKey = serverPath;
				process.stdout.write(".");
			}
		}
	}
	return segments;
}

async function importBook(bookFolder) {
	const bundleFolder = path.join(DIST_DIR, bookFolder);
	const bundlePath = path.join(bundleFolder, "bundle.json");

	if (!fs.existsSync(bundlePath)) return;

	console.log(`\n📚 IMPORTING: ${bookFolder}`);
	const data = await fs.readJson(bundlePath);

	// XÁC ĐỊNH ASSET ROOT
	// Nếu _assetsRoot là "." thì trỏ vào chính folder bundle đó
	let assetsRoot = data._assetsRoot;
	if (assetsRoot === ".") {
		assetsRoot = bundleFolder;
	} else if (!path.isAbsolute(assetsRoot)) {
		assetsRoot = path.join(bundleFolder, assetsRoot);
	}

	try {
		console.log("   Creating Series...");
		const seriesPayload = {
			title: data.title,
			metadata: {
				...data.metadata,
				description: data.metadata.description,
				coverImageUrl: null,
			},
		};

		const seriesRes = await api.post("/series", seriesPayload);
		const seriesId = seriesRes.data.id;

		if (data.metadata.coverImageUrl) {
			console.log("   Uploading Cover...");
			const coverLocalPath = path.resolve(
				assetsRoot,
				data.metadata.coverImageUrl,
			);
			const coverServerPath = await uploadAsset(coverLocalPath, seriesId);

			if (coverServerPath) {
				seriesPayload.metadata.coverImageUrl = coverServerPath;
				await api.put(`/series/${seriesId}`, seriesPayload);
			}
		}

		for (const vol of data.volumes) {
			console.log(`   📂 Volume: ${vol.title}`);
			const volRes = await api.post("/volume", {
				seriesId: seriesId,
				title: vol.title,
				order: vol.order,
			});
			const volumeId = volRes.data.id;

			console.log(`      Processing ${vol.chapters.length} chapters `);
			for (const chap of vol.chapters) {
				await processSegments(chap.content, assetsRoot, seriesId);

				if (chap.footnotes) {
					for (const note of chap.footnotes) {
						await processSegments(
							note.segments,
							assetsRoot,
							seriesId,
						);
					}
				}

				await api.post("/chapter", {
					volumeId: volumeId,
					title: chap.title,
					order: chap.order,
					content: chap.content,
					footnotes: chap.footnotes || [],
				});
			}
			console.log(" [Done]");
		}
		console.log(`✅ Success: ${data.title}`);
	} catch (e) {
		console.log(`\n❌ Failed: ${e.message}`);
	}
}

(async () => {
	if (!fs.existsSync(DIST_DIR)) return console.log("No dist folder found.");
	const books = fs.readdirSync(DIST_DIR);
	for (const book of books) {
		if (fs.lstatSync(path.join(DIST_DIR, book)).isDirectory()) {
			await importBook(book);
		}
	}
})();

