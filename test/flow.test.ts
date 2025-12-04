import fs from "fs";
import p from "pactum";
import path from "path";

const baseUrl = "http://localhost:5062/api/v1";
const assetsDir = path.join(__dirname, "assets");

const readJson = (fileName: string) => {
	const filePath = path.join(assetsDir, fileName);
	const rawData = fs.readFileSync(filePath, "utf-8");
	return JSON.parse(rawData);
};

describe("Grimoire Full Flow E2E (Data Driven)", () => {
	it("1. Should create Series from metadata.json", async () => {
		const seriesData = readJson("metadata.json");

		await p
			.spec()
			.post(`${baseUrl}/series`)
			.withJson(seriesData)
			.expectStatus(201)
			.stores("SeriesId", "id");
	});

	it("2. Should create Volume from volume.json using SeriesId", async () => {
		const volumeData = readJson("volume.json");
		volumeData.seriesId = "$S{SeriesId}";

		await p
			.spec()
			.post(`${baseUrl}/volume`)
			.withJson(volumeData)
			.expectStatus(201)
			.stores("VolumeId", "id");
	});

	it("3. Should create Chapter from chapter.json using VolumeId", async () => {
		const chapterData = readJson("chapter.json");
		chapterData.volumeId = "$S{VolumeId}";

		await p
			.spec()
			.post(`${baseUrl}/chapter`)
			.withJson(chapterData)
			.expectStatus(201)
			// CẦN THÊM: Lưu ChapterId để xóa ở bước Cleanup
			.stores("ChapterId", "id");
	});

	it("4. Should upload Cover Image (main_cover.jpg)", async () => {
		const imagePath = path.join(assetsDir, "main_cover.jpg");

		if (!fs.existsSync(imagePath)) {
			throw new Error(`File not found: ${imagePath}`);
		}

		await p
			.spec()
			.post(`${baseUrl}/file/upload/$S{SeriesId}`)
			.withQueryParams("file", "Cover") // Giữ nguyên query param như logic cũ
			// SỬA ĐỔI: Dùng .withFile của Pactum (key là 'file', đường dẫn ảnh)
			.withFile("file", imagePath)
			.expectStatus(200)
			.stores("AssetPath", "path"); // Lưu trường 'path' từ JSON trả về
	});

	it("5. Should update Series Cover with uploaded image", async () => {
		const updateData = readJson("metadata.json");
		updateData.metadata.coverImage = "$S{AssetPath}";

		await p
			.spec()
			.put(`${baseUrl}/series/$S{SeriesId}`)
			.withJson(updateData)
			.expectStatus(200);
	});

	it("6. Cleanup (Chapter -> Volume -> Series)", async () => {
		// 1. Xóa Chapter trước
		await p
			.spec()
			.delete(`${baseUrl}/chapter/$S{ChapterId}`)
			.expectStatus(200);

		// 2. Xóa Volume
		await p
			.spec()
			.delete(`${baseUrl}/volume/$S{VolumeId}`)
			.expectStatus(200);

		// 3. Xóa Series cuối cùng
		await p
			.spec()
			.delete(`${baseUrl}/series/$S{SeriesId}`)
			.expectStatus(200);
	});
});
