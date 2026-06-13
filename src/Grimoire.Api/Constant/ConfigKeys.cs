namespace Grimoire.Api.Constant;

public static class ConfigKeys {
	public const string ConnectionStringsPostgre = "ConnectionStrings:Postgre";
	public const string ConnectionStringName = "Postgre";
	public const string OpenApiServerUrl = "OpenApi:ServerUrl";
	public const string CorsPolicyAllowAll = "AllowAll";
	public const string HangfireQueueDefault = "default";
	public const string HangfireQueueExports = "exports";
}

public static class ProblemDetailsKeys {
	public const string Errors = "errors";
	public const string TraceId = "traceId";
}

public static class ContentTypes {
	public const string ProblemJson = "application/problem+json";
}
