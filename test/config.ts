const rawUrl = process.env.GRIMOIRE_BLACKBOX_BASE_URL ?? "http://localhost:5062/api/v1";
export const API_BASE_URL = rawUrl.replace(/\/$/, "");
