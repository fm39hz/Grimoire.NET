namespace Grimoire.Infrastructure.Configuration;

/// <summary>
///     Configuration for S3-compatible storage.
///     Sensitive fields (AccessKey, SecretKey) should be set via
///     User Secrets (dev) or environment variables (production).
/// </summary>
public class S3Configuration {
	public const string SECTION_NAME = "Storage:S3";

	public string Endpoint { get; set; } = "http://localhost:9000";
	public string BucketName { get; set; } = "grimoire";
	public string AccessKey { get; set; } = string.Empty;
	public string SecretKey { get; set; } = string.Empty;
	public bool UseSsl { get; set; }
	public string Region { get; set; } = "us-east-1";
}
