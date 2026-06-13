namespace Grimoire.Api.Constant;

public static class SseConstants {
	public const string ContentType = "text/event-stream";
	public const string CacheControl = "no-cache";
	public const string Connection = "keep-alive";
	public const string DataPrefix = "data: ";
	public const string LineBreak = "\n\n";
	public const string StatusCompleted = "Completed";
	public const string StatusFailed = "Failed";
	public const string StatusNotFound = "NotFound";
	public const string StatusQueued = "Queued";
}
